using System.Diagnostics.CodeAnalysis;
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

    private (DateTime Expiery, string Token) TokenData;

    private (DateTime Expiery, string JWT) JWTData;

    private const int ErrorResponseMaxLogSize = 200;

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

    private string _authToken = "";
    private string _repository = "";
    private string _owner = "";
    private int _maxRetries;

    #endregion

    public void Initialize()
    {
        _cfg.OnValueChanged(CCVars.GithubAuthToken, val => SetValueAndInitHttpClient(ref _authToken, val), true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryName, val => SetValueAndInitHttpClient(ref _repository, val), true);
        _cfg.OnValueChanged(CCVars.GithubRepositoryOwner, val => SetValueAndInitHttpClient(ref _owner, val), true);
        _cfg.OnValueChanged(CCVars.GithubMaxRetries, val => SetValueAndInitHttpClient(ref _maxRetries, val), true);

        _sawmill = _log.GetSawmill("github");
    }

    private void SetValueAndInitHttpClient<T>(ref T toSet, T value)
    {
        Interlocked.Exchange(ref toSet, value);

        if(!HaveFullApiData())
            return;

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

        if (request.AuthenticationMethodMethod == AuthMethod.Token)
        {
            await EnsureTokenNotExpired();
        }

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

        _sawmill.Error(responseText);

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
        var json = JsonSerializer.Serialize(request);
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
        // httpRequest.RequestUri = new Uri("https://api.github.com/app");
        return httpRequest;
    }

    private bool HaveFullApiData()
    {
        return !string.IsNullOrWhiteSpace(_authToken) &&
               !string.IsNullOrWhiteSpace(_repository) &&
               !string.IsNullOrWhiteSpace(_owner);
    }

    private string CreateAuthenticationHeader(IGithubRequest request)
    {
        if (request.AuthenticationMethodMethod == AuthMethod.Token)
            return AuthHeaderBearer + TokenData.Token;
        if (request.AuthenticationMethodMethod == AuthMethod.JWT)
            return AuthHeaderBearer + GetValidJWT();

        throw new Exception("Unknown auth method");
    }

    private async Task EnsureTokenNotExpired()
    {
        if (TokenData.Expiery > DateTime.UtcNow)
            return;

        _sawmill.Info("Token expired - requesting new token!");

        var installationRequest = new InstallationsRequest();
        var installationHttpResponse = await TryMakeRequestSafe(installationRequest, CancellationToken.None);
        if (installationHttpResponse == null)
            return;

        var parsedResponse = await installationHttpResponse.Content.ReadFromJsonAsync<List<GithubInstallation>>();

        if (parsedResponse == null)
            return;

        if (parsedResponse.Count == 0)
        {
            _sawmill.Error("App not installed anywhere.");
        }

        if (parsedResponse.Count > 1)
        {
            _sawmill.Error("App installed in more than one location");
        }

        var tokenLocation = parsedResponse[0].AccessToken;

        var tokenRequest = new TokenRequest
        {
            Location = tokenLocation.Split("https://api.github.com/")[1],
        };

        var tokenHttpResponse = await TryMakeRequestSafe(tokenRequest, CancellationToken.None);

        if (tokenHttpResponse == null)
            return;

        var tokenResponse = await tokenHttpResponse.Content.ReadFromJsonAsync<TokenResponse>();

        if (tokenResponse == null)
            return;

        TokenData = (tokenResponse.Exp, tokenResponse.Token);
    }

    // TODO: YOU CAN GET PERMISSIONS FROM THE https://api.github.com/app ENDPOINT. ENSURE PERMISSIONS ARE THE SAME
    // See: https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-json-web-token-jwt-for-a-github-app
    private string GetValidJWT()
    {
        if (JWTData.Expiery > DateTime.UtcNow)
            return JWTData.JWT;

        var clientID = _cfg.GetCVar(CCVars.GithubAppId);
        var keyPem = _cfg.GetCVar(CCVars.GithubAppPrivateKey);

        //todo: fix this sus
        var time = DateTime.UtcNow;
        var expTime = time + TimeSpan.FromMinutes(10);

        var iat = ((DateTimeOffset)(time - TimeSpan.FromMinutes(2))).ToUnixTimeSeconds();
        var exp = ((DateTimeOffset) expTime).ToUnixTimeSeconds();

        var header_json =
"""
{
    "typ":"JWT",
    "alg":"RS256"
}
""";

        var header_encoded = Base64Encode(header_json);

        var payload_json =
$$"""
{
    "iat":{{iat}},
    "exp":{{exp}},
    "iss":"{{clientID}}"
}
""";

        var payload_json_encoded = Base64Encode(payload_json);

        var head_payload = $"{header_encoded}.{payload_json_encoded}";

        var RSA = System.Security.Cryptography.RSA.Create();
        RSA.ImportFromPem(keyPem);

        // var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(head_payload));
        // var signedData = RSA.SignData(System.Text.Encoding.Unicode.GetBytes(head_payload), HashAlgorithmName.SHA256);

        var bytesPlainTextData = Encoding.UTF8.GetBytes(head_payload);

        var signedData = RSA.SignData(bytesPlainTextData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signBase64 = Convert.ToBase64String(signedData).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var JWT = $"{head_payload}.{signBase64}";

        JWTData = (expTime, JWT);

        _sawmill.Info("Generated new JWT.");

        return JWT;
    }

    private string Base64Encode(string plainText)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText)).TrimEnd('=').Replace('+', '-')
            .Replace('/', '_');
    }

    #endregion
}
