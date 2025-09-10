using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Starlight.CCVar;

namespace Content.Server._NullLink.PlayerData;
public sealed partial class NullLinkPlayerManager
{
    private const string _scope = "identify%20guilds%20guilds.members.read";
    private string? _discordKey;
    private string? _discordCallback;
    private string? _secret;

    public void InitializeLinking()
    {
        _discordKey = _cfg.GetCVar(StarlightCCVars.DiscordKey);
        _discordCallback = _cfg.GetCVar(StarlightCCVars.DiscordCallback);
        _secret = _cfg.GetCVar(StarlightCCVars.Secret);
    }
    public string GetDiscordAuthUrl(string customState)
    {
        if (string.IsNullOrEmpty(_discordCallback) || string.IsNullOrEmpty(_discordKey) || string.IsNullOrEmpty(_secret)) 
            return "";

        var secretKeyBytes = Encoding.UTF8.GetBytes(_secret);
        using var hmac = new HMACSHA256(secretKeyBytes);

        var dataBytes = Encoding.UTF8.GetBytes(customState);
        var hashBytes = hmac.ComputeHash(dataBytes);
        var state = $"{customState}|{BitConverter.ToString(hashBytes).Replace("-", "").ToLower()}";
        var encodedState = Uri.EscapeDataString(state);

        return $"https://discord.com/api/oauth2/authorize?client_id={_discordKey}&redirect_uri={Uri.EscapeDataString(_discordCallback)}&response_type=code&scope={_scope}&state={encodedState}";
    }
}
