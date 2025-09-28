using System;
using System.Linq;
using System.Threading;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Shared.Starlight;
using Content.Shared.Starlight.CCVar;
using Robust.Shared;
using Robust.Shared.Localization;
using static Content.Shared.Administration.Notes.AdminMessageEuiState;

namespace Content.Server.GameTicking;
public sealed partial class GameTicker //🌟Starlight🌟
{

    #region Starlight
    [ViewVariables]
    public string GamemodeNameOverride = "";
    [ViewVariables]
    public string GamemodeDescOverride = "";
    #endregion

    private WebhookIdentifier? _statusWebhookIdentifier;
    private WebhookIdentifier? _statusWebhookStaffIdentifier;
    private ulong _statusMessageId = 0;
    private ulong _statusStaffMessageId = 0;
    private Timer _timer = null!;
    private string _serverName = "";
    private WebhookPayload _payload = new()
    {
        Embeds = new List<WebhookEmbed>
                {
                    new()
                    {
                        Color = 65403,
                        Footer = new WebhookEmbedFooter
                        {
                            Text = "",
                            IconUrl = "https://ss14-starlight.online/favicon.png"
                        },
                        Fields = [
                            new(){
                               Name  = "Players",
                               Inline = true,
                               Value = "0/0"
                            },
                            new(){
                               Name  = "Map",
                               Inline = true,
                               Value = ""
                            },
                            new(){
                               Name  = "Game mode",
                               Inline = true,
                               Value = ""
                            },
                            new(){
                               Name  = "Round duration",
                               Inline = true,
                               Value = ""
                            },
                            new(){
                               Name  = "Panic Bunker",
                               Inline = true,
                               Value = "N/A"
                            }
                        ],
                        Thumbnail = new WebhookEmbedImage
                        {
                            Url = "https://ss14-starlight.online/favicon.png"
                        }
                    },
                },
    };
    private WebhookPayload _payloadWithAdmins = new()
    {
        Embeds = new List<WebhookEmbed>
                {
                    new()
                    {
                        Color = 65403,
                        Footer = new WebhookEmbedFooter
                        {
                            Text = "",
                            IconUrl = "https://ss14-starlight.online/favicon.png"
                        },
                        Fields = [
                            new(){
                               Name  = "Players",
                               Inline = true,
                               Value = "0/0"
                            },
                            new(){
                               Name  = "Admins",
                               Inline = true,
                               Value = ""
                            },
                            new(){
                               Name  = "Mentors",
                               Inline = true,
                               Value = ""
                            },
                            new(){
                               Name  = "Map",
                               Inline = true,
                               Value = ""
                            },
                            new(){
                               Name  = "Game mode",
                               Inline = true,
                               Value = ""
                            },
                            new(){
                               Name  = "Round duration",
                               Inline = true,
                               Value = ""
                            },
                            new(){
                               Name  = "Panic Bunker",
                               Inline = true,
                               Value = "N/A"
                            }
                        ],
                        Thumbnail = new WebhookEmbedImage
                        {
                            Url = "https://ss14-starlight.online/favicon.png"
                        }
                    },
                },
    };
    private void StarlightSubs()
    {
        Subs.CVar(_cfg, StarlightCCVars.StatusWebhook, value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
                _discord.GetWebhook(value, data => _statusWebhookIdentifier = data.ToIdentifier());
        }, true);
        Subs.CVar(_cfg, StarlightCCVars.StatusStaffWebhook, value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
                _discord.GetWebhook(value, data => _statusWebhookStaffIdentifier = data.ToIdentifier());
        }, true);
        Subs.CVar(_cfg, StarlightCCVars.StatusMessageId, v => _statusMessageId = v, true);
        Subs.CVar(_cfg, StarlightCCVars.StatusMessageStaffId, v => _statusStaffMessageId = v, true);
        Subs.CVar(_cfg, CVars.GameHostName, v => _serverName = v[..Math.Min(v.Length, 1500)], true);
        Subs.CVar(_cfg, StarlightCCVars.OverrideGamemodeName, v => GamemodeNameOverride = v, true);
        Subs.CVar(_cfg, StarlightCCVars.OverrideGamemodeDescription, v => GamemodeDescOverride = v, true);


        _timer = new(StarlightStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void StarlightStatus(object? _)
    {
        StarlightWithAdminStatus();
        if (_statusWebhookIdentifier is null) return;

        var mapName = _gameMapManager.GetSelectedMap()?.MapName ?? Loc.GetString("discord-round-notifications-unknown-map");
        var preset = CurrentPreset?.ModeTitle is string title && title != "????" ? Loc.GetString(title) : "????";
        var embed = _payload.Embeds![0];

        embed.Footer = new WebhookEmbedFooter
        {
            Text = $"{_serverName} ({RoundId})",
            IconUrl = "https://ss14-starlight.online/favicon.png"
        };

        embed.Fields[0] = embed.Fields[0] with { Value = $"{_playerManager.PlayerCount}/{_playerManager.MaxPlayers}" };
        embed.Fields[1] = embed.Fields[1] with { Value = mapName };
        embed.Fields[2] = embed.Fields[2] with { Value = string.IsNullOrWhiteSpace(GamemodeDescOverride) ? preset : Loc.GetString(GamemodeDescOverride) };
        embed.Fields[3] = embed.Fields[3] with { Value = RoundDuration().ToString("hh\\:mm\\:ss") };
        embed.Fields[4] = embed.Fields[4] with { Value = _admin.PanicBunker.Enabled ? "On" : "Off" };

        _payload.Embeds[0] = embed;

        if (_statusMessageId == 0)
        {
            _ = _discord.CreateMessage(_statusWebhookIdentifier.Value, _payload);
            _statusWebhookIdentifier = null;
        }
        else
            _ = _discord.EditMessage(_statusWebhookIdentifier.Value, _statusMessageId, _payload);
    }
    private void StarlightWithAdminStatus()
    {
        if (_statusWebhookStaffIdentifier is null) return;

        var mapName = _gameMapManager.GetSelectedMap()?.MapName ?? Loc.GetString("discord-round-notifications-unknown-map");
        var preset = CurrentPreset?.ModeTitle is string title && title != "????" ? Loc.GetString(title) : "????";
        var embed = _payloadWithAdmins.Embeds![0];

        embed.Footer = new WebhookEmbedFooter
        {
            Text = $"{_serverName} ({RoundId})",
            IconUrl = "https://ss14-starlight.online/favicon.png"
        };
        embed.Fields[0] = embed.Fields[0] with { Value = $"{_playerManager.PlayerCount}/{_playerManager.MaxPlayers}" };
        embed.Fields[1] = embed.Fields[1] with { Value = $"{_adminManager.ActiveAdmins.Count()}/{_adminManager.AllAdmins.Count()}" };
        embed.Fields[2] = embed.Fields[2] with { Value = _playerRolesManager.Mentors.Count().ToString() };
        embed.Fields[3] = embed.Fields[3] with { Value = mapName };
        embed.Fields[4] = embed.Fields[4] with { Value = preset };
        embed.Fields[5] = embed.Fields[5] with { Value = RoundDuration().ToString("hh\\:mm\\:ss") };
        embed.Fields[6] = embed.Fields[6] with { Value = _admin.PanicBunker.Enabled ? "On" : "Off" };

        _payloadWithAdmins.Embeds[0] = embed;

        if (_statusStaffMessageId == 0)
        {
            _ = _discord.CreateMessage(_statusWebhookStaffIdentifier.Value, _payloadWithAdmins);
            _statusWebhookStaffIdentifier = null;
        }
        else
            _ = _discord.EditMessage(_statusWebhookStaffIdentifier.Value, _statusStaffMessageId, _payloadWithAdmins);
    }
}
