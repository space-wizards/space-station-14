using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Github.Requests;
using Content.Server.Github.Responses;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;

namespace Content.Server.Github;

/// <summary>
/// Basic implementation of the GitHub api. This was mainly created for making issues from users bug reports - it is not
/// a full implementation! I tried to follow the spec very closely and the docs are really well done. I highly recommend
/// taking a look at them!
/// <br/>
/// <br/> Some useful information about the api:
/// <br/> <see href="https://docs.github.com/en/rest?apiVersion=2022-11-28">Api home page</see>
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28">Best practices</see>
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28">Rate limit information</see>
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/troubleshooting-the-rest-api?apiVersion=2022-11-28">Troubleshooting</see>
/// </summary>
/// <remarks>As it uses async, it should be called from background worker when possible, like <see cref="GithubBackgroundWorker"/>.</remarks>
public sealed class GithubClient
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private HttpClient _httpClient = default!;

    private ISawmill _sawmill = default!;

    // Token data for the GitHub app (This is used to authenticate stuff like new issue creation)
    private (DateTime? Expiery, string Token) _tokenData;

    // Json web token for the GitHub app (This is used to authenticate stuff like seeing where the app is installed)
    // The token is created locally.
    private (DateTime? Expiery, string JWT) _jwtData;

    private const int ErrorResponseMaxLogSize = 200;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // Docs say 10 should be the maximum.
    private readonly TimeSpan _jwtExpiration = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _jwtBackDate = TimeSpan.FromMinutes(1);

    // Buffers because requests can take a while. We don't want the tokens to expire in the middle of doing requests!
    private readonly TimeSpan _jwtBuffer = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _tokenBuffer = TimeSpan.FromMinutes(2);

    private string _privateKey = "";

    #region Header constants

    private const string ProductName = "SpaceStation14GithubApi";
    private const string ProductVersion = "1";

    private const string AcceptHeader = "Accept";
    private const string AcceptHeaderType = "application/vnd.github+json";

    private const string AuthHeader = "Authorization";
    private const string AuthHeaderBearer = "Bearer ";

    private const string VersionHeader = "X-GitHub-Api-Version";
    private const string VersionNumber = "2022-11-28";

    #endregion

    private readonly Uri _baseUri = new("https://api.github.com/");

    #region CCvar values

    private string _appId = "";
    private string _repository = "";
    private string _owner = "";
    private int _maxRetries;

    #endregion

    public void Initialize()
    {
        _sawmill = _log.GetSawmill("github");
        _tokenData = (null, "");
        _jwtData = (null, "");

        _cfg.OnValueChanged(CCVars.GithubAppPrivateKeyPath, OnPrivateKeyPathChanged, true);
        _cfg.OnValueChanged(CCVars.GithubAppId, val => Interlocked.Exchange(ref _appId, val), true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryName, val => Interlocked.Exchange(ref _repository, val), true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryOwner, val => Interlocked.Exchange(ref _owner, val), true);
        _cfg.OnValueChanged(CCVars.GithubMaxRetries, val => SetValueAndInitHttpClient(ref _maxRetries, val), true);
    }

    private void OnPrivateKeyPathChanged(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        if (!File.Exists(path))
        {
            _sawmill.Error($"\"{path}\" does not exist.");
            return;
        }

        string fileText;
        try
        {
            fileText = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            _sawmill.Error($"\"{path}\" could not be read!\n{e}");
            return;
        }

        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(fileText);
        }
        catch
        {
            _sawmill.Error($"\"{path}\" does not contain a valid private key!");
            return;
        }

        _privateKey = fileText;
    }

    private void SetValueAndInitHttpClient<T>(ref T toSet, T value)
    {
        Interlocked.Exchange(ref toSet, value);

        var httpMessageHandler = new RetryHandler(new HttpClientHandler(), _maxRetries, _sawmill);
        var newClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = _baseUri,
            DefaultRequestHeaders =
            {
                { AcceptHeader, AcceptHeaderType },
                { VersionHeader, VersionNumber },
            },
            Timeout = TimeSpan.FromSeconds(15),
        };

        newClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ProductName, ProductVersion));

        Interlocked.Exchange(ref _httpClient, newClient);
    }

    #region Public functions

    /// <summary>
    /// The standard way to make requests to the GitHub api. This will ensure that the request respects the rate limit
    /// and will also retry the request if it fails. Awaiting this to finish could take a very long time depending
    /// on what exactly is going on! Only await for it if you're willing to wait a long time.
    /// </summary>
    /// <param name="request">The request you want to make.</param>
    /// <param name="ct">Token for operation cancellation.</param>
    /// <returns>The direct HTTP response from the API. If null the request could not be made.</returns>
    public async Task<HttpResponseMessage?> TryMakeRequestSafe(IGithubRequest request, CancellationToken ct)
    {
        if (!HaveFullApiData())
        {
            _sawmill.Info("Tried to make a github api request but the api was not enabled.");
            return null;
        }

        if (request.AuthenticationMethod == GithubAuthMethod.Token && !await TryEnsureTokenNotExpired(ct))
            return null;

        return await MakeRequest(request, ct);
    }

    private async Task<HttpResponseMessage?> MakeRequest(IGithubRequest request, CancellationToken ct)
    {
        var httpRequestMessage = BuildRequest(request);

        var response = await _httpClient.SendAsync(httpRequestMessage, ct);

        var message = $"Made a github api request to: '{httpRequestMessage.RequestUri}', status is {response.StatusCode}";
        if (response.IsSuccessStatusCode)
        {
            _sawmill.Info(message);
            return response;
        }

        _sawmill.Error(message);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (responseText.Length > ErrorResponseMaxLogSize)
            responseText = responseText.Substring(0, ErrorResponseMaxLogSize);

        _sawmill.Error(message + "\r\n" + responseText);

        return null;
    }

    /// <summary>
    /// A simple helper function that just tries to parse a header value that is expected to be a long int.
    /// In general, there are just a lot of single value headers that are longs so this removes a lot of duplicate code.
    /// </summary>
    /// <param name="headers">The headers that you want to search.</param>
    /// <param name="header">The header you want to get the long value for.</param>
    /// <param name="value">Value of header, if found, null otherwise.</param>
    /// <returns>The headers value if it exists, null otherwise.</returns>
    public static bool TryGetHeaderAsLong(HttpResponseHeaders? headers, string header, [NotNullWhen(true)] out long? value)
    {
        value = null;
        if (headers == null)
            return false;

        if (!headers.TryGetValues(header, out var headerValues))
            return false;

        if (!long.TryParse(headerValues.First(), out var result))
            return false;

        value = result;
        return true;
    }

    # endregion

    #region Helper functions

    private HttpRequestMessage BuildRequest(IGithubRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonSerializerOptions);
        var payload = new StringContent(json, Encoding.UTF8, "application/json");

        var builder = new UriBuilder(_baseUri)
        {
            Port = -1,
            Path = request.GetLocation(_owner, _repository),
        };

        var httpRequest = new HttpRequestMessage
        {
            Method = request.RequestMethod,
            RequestUri = builder.Uri,
            Content = payload,
        };

        httpRequest.Headers.Add(AuthHeader, CreateAuthenticationHeader(request));

        return httpRequest;
    }

    private bool HaveFullApiData()
    {
        return !string.IsNullOrWhiteSpace(_privateKey) &&
               !string.IsNullOrWhiteSpace(_repository) &&
               !string.IsNullOrWhiteSpace(_owner);
    }

    private string CreateAuthenticationHeader(IGithubRequest request)
    {
        return request.AuthenticationMethod switch
        {
            GithubAuthMethod.Token => AuthHeaderBearer + _tokenData.Token,
            GithubAuthMethod.JWT => AuthHeaderBearer + GetValidJwt(),
            _ => throw new Exception("Unknown auth method!"),
        };
    }

    // TODO: Maybe ensure that perms are only read metadata / write issues so people don't give full access
    /// <summary>
    /// Try to get a valid verification token from the GitHub api
    /// </summary>
    /// <returns>True if the token is valid and successfully found, false if there was an error.</returns>
    private async Task<bool> TryEnsureTokenNotExpired(CancellationToken ct)
    {
        if (_tokenData.Expiery != null && _tokenData.Expiery - _tokenBuffer > DateTime.UtcNow)
            return true;

        _sawmill.Info("Token expired - requesting new token!");

        var installationRequest = new InstallationsRequest();
        var installationHttpResponse = await MakeRequest(installationRequest, ct);
        if (installationHttpResponse == null)
        {
            _sawmill.Error("Could not make http installation request when creating token.");
            return false;
        }

        var installationResponse = await installationHttpResponse.Content.ReadFromJsonAsync<List<InstallationResponse>>(_jsonSerializerOptions, ct);
        if (installationResponse == null)
        {
            _sawmill.Error("Could not parse installation response.");
            return false;
        }

        if (installationResponse.Count == 0)
        {
            _sawmill.Error("App not installed anywhere.");
            return false;
        }

        int? installationId = null;
        foreach (var installation in installationResponse)
        {
            if (installation.Account.Login != _owner)
                continue;

            installationId = installation.Id;
            break;
        }

        if (installationId == null)
        {
            _sawmill.Error("App not installed in given repository.");
            return false;
        }

        var tokenRequest = new TokenRequest
        {
            InstallationId = installationId.Value,
        };

        var tokenHttpResponse = await MakeRequest(tokenRequest, ct);
        if (tokenHttpResponse == null)
        {
            _sawmill.Error("Could not make http token request when creating token..");
            return false;
        }

        var tokenResponse = await tokenHttpResponse.Content.ReadFromJsonAsync<TokenResponse>(_jsonSerializerOptions, ct);
        if (tokenResponse == null)
        {
            _sawmill.Error("Could not parse token response.");
            return false;
        }

        _tokenData = (tokenResponse.ExpiresAt, tokenResponse.Token);
        return true;
    }

    // See: https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-json-web-token-jwt-for-a-github-app
    private string GetValidJwt()
    {
        if (_jwtData.Expiery != null && _jwtData.Expiery - _jwtBuffer > DateTime.UtcNow)
            return _jwtData.JWT;

        var githubClientId = _appId;
        var apiPrivateKey = _privateKey;

        var time = DateTime.UtcNow;
        var expTime = time + _jwtExpiration;
        var iatTime = time - _jwtBackDate;

        var iat = ((DateTimeOffset) iatTime).ToUnixTimeSeconds();
        var exp = ((DateTimeOffset) expTime).ToUnixTimeSeconds();

        const string headerJson = """
                                  {
                                      "typ":"JWT",
                                      "alg":"RS256"
                                  }
                                  """;

        var headerEncoded = Base64EncodeUrlSafe(headerJson);

        var payloadJson = $$"""
                            {
                                "iat":{{iat}},
                                "exp":{{exp}},
                                "iss":"{{githubClientId}}"
                            }
                            """;

        var payloadJsonEncoded = Base64EncodeUrlSafe(payloadJson);

        var headPayload = $"{headerEncoded}.{payloadJsonEncoded}";

        var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(apiPrivateKey);

        var bytesPlainTextData = Encoding.UTF8.GetBytes(headPayload);

        var signedData = rsa.SignData(bytesPlainTextData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signBase64 = Base64EncodeUrlSafe(signedData);

        var jwt = $"{headPayload}.{signBase64}";

        _jwtData = (expTime, jwt);

        _sawmill.Info("Generated new JWT.");

        return jwt;
    }

    private string Base64EncodeUrlSafe(string plainText)
    {
        return Base64EncodeUrlSafe(Encoding.UTF8.GetBytes(plainText));
    }

    private string Base64EncodeUrlSafe(byte[] plainText)
    {
        return Convert.ToBase64String(plainText)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    #endregion
}
