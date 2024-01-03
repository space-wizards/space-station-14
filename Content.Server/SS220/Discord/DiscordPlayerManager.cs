// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.Discord;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.Discord;

public sealed class DiscordPlayerManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;


    private ISawmill _sawmill = default!;
    private readonly HttpClient _httpClient = new();

    private string _apiUrl = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("DiscordPlayerManager");

        _netMgr.RegisterNetMessage<MsgUpdatePlayerDiscordStatus>();

        _cfg.OnValueChanged(CCCVars.DiscordAuthApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCCVars.DiscordAuthApiKey, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", v);
        },
        true);
    }

    void IPostInjectInit.PostInject()
    {
        _playerManager.PlayerStatusChanged += PlayerManager_PlayerStatusChanged; ;
    }

    private async void PlayerManager_PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            await UpdateUserDiscordRolesStatus(e);
        }
    }

    private async Task UpdateUserDiscordRolesStatus(SessionStatusEventArgs e)
    {
        var info = await GetSponsorInfo(e.Session.UserId);

        if (info is not null)
        {
            _netMgr.ServerSendMessage(new MsgUpdatePlayerDiscordStatus
            {
                Info = info
            },
            e.Session.Channel);
        }
    }

    private async Task<DiscordSponsorInfo?> GetSponsorInfo(NetUserId userId)
    {
        if (string.IsNullOrEmpty(_apiUrl))
        {
            return null;
        }

        try
        {
            var url = $"{_apiUrl}/userinfo/{userId.UserId}";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to get player sponsor info: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<DiscordSponsorInfo>(GetJsonSerializerOptions());
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }

        return null;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var opt = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        opt.Converters.Add(new JsonStringEnumConverter());

        return opt;
    }

    /// <summary>
    /// Проверка, генерация ключа для дискорда.
    /// </summary>
    /// <param name="playerData"></param>
    /// <returns></returns>
    public async Task<string> CheckAndGenerateKey(SessionData playerData)
    {
        try
        {
            var userId = playerData.UserId;

            var existing = await _db.GetAccountDiscordLink(playerData.UserId);

            // Привязки не существует, создаём.
            if (existing is null)
            {
                return await CreateKey(userId);
            }

            // Привязка существует и ключа нет, значит аккаунт уже прошёл привязку.
            if (string.IsNullOrWhiteSpace(existing.HashKey))
            {
                return string.Empty;
            }

            // Привязка существует и есть ключ, значит пользователь запрашивал привязку, но не использовал ключ.
            return existing.HashKey;
        }
        catch (Exception ex)
        {
            _sawmill.Log(LogLevel.Error, ex, "Ошибка во время проверки и генерации ключа");
            throw;
        }
    }

    private async Task<string> CreateKey(Guid userId)
    {
        var discordPlayer = new DiscordPlayer
        {
            SS14Id = userId,
            HashKey = CreateSecureRandomString(8)
        };

        await _db.InsertDiscord(discordPlayer);

        return discordPlayer.HashKey;
    }

    private static string CreateSecureRandomString(int count = 32)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
    }
}

