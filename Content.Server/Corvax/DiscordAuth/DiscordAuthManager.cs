using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.DiscordAuth;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Corvax.DiscordAuth;

// TODO: Add minimal Discord account age check for panic bunker by extracting timestamp from snowflake received from API secured with key

/// <summary>
///     Manage Discord linking with SS14 account through external API
/// </summary>
public sealed class DiscordAuthManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IPlayerManager _playerMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;
    private readonly HttpClient _httpClient = new();
    private bool _isEnabled = false;
    private string _apiUrl = string.Empty;
    private string _apiKey = string.Empty;

    /// <summary>
    ///     Raised when player passed verification or if feature disabled
    /// </summary>
    public event EventHandler<ICommonSession>? PlayerVerified;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("discord_auth");

        _cfg.OnValueChanged(CCCVars.DiscordAuthEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CCCVars.DiscordAuthApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCCVars.DiscordAuthApiKey, v => _apiKey = v, true);

        _netMgr.RegisterNetMessage<MsgDiscordAuthRequired>();
        _netMgr.RegisterNetMessage<MsgDiscordAuthCheck>(OnAuthCheck);

        _playerMgr.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private async void OnAuthCheck(MsgDiscordAuthCheck message)
    {
        var isVerified = await IsVerified(message.MsgChannel.UserId);
        if (isVerified)
        {
            var session = _playerMgr.GetSessionById(message.MsgChannel.UserId);

            PlayerVerified?.Invoke(this, session);
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected)
            return;

        if (!_isEnabled)
        {
            PlayerVerified?.Invoke(this, e.Session);
            return;
        }

        if (e.NewStatus == SessionStatus.Connected)
        {
            var isVerified = await IsVerified(e.Session.UserId);
            if (isVerified)
            {
                PlayerVerified?.Invoke(this, e.Session);
                return;
            }

            var authUrl = await GenerateAuthLink(e.Session.UserId);
            var msg = new MsgDiscordAuthRequired() { AuthUrl = authUrl };
            e.Session.Channel.SendMessage(msg);
        }
    }

    public async Task<string> GenerateAuthLink(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Info($"Player {userId} requested generation Discord verification link");

        var requestUrl = $"{_apiUrl}/{WebUtility.UrlEncode(userId.ToString())}?key={_apiKey}";
        var response = await _httpClient.PostAsync(requestUrl, null, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordGenerateLinkResponse>(cancellationToken: cancel);
        return data!.Url;
    }

    public async Task<bool> IsVerified(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Debug($"Player {userId} check Discord verification");

        var requestUrl = $"{_apiUrl}/{WebUtility.UrlEncode(userId.ToString())}";
        var response = await _httpClient.GetAsync(requestUrl, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordAuthInfoResponse>(cancellationToken: cancel);
        return data!.IsLinked;
    }

    [UsedImplicitly]
    private sealed record DiscordGenerateLinkResponse(string Url);
    [UsedImplicitly]
    private sealed record DiscordAuthInfoResponse(bool IsLinked);
}
