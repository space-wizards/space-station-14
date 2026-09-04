using Content.Server.Discord;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    [ViewVariables]
    public bool LobbyEnabled { get; private set; }

    [ViewVariables]
    public bool DummyTicker { get; private set; } = false;

    [ViewVariables]
    public TimeSpan LobbyDuration { get; private set; } = TimeSpan.Zero;

    [ViewVariables]
    public bool DisallowLateJoin { get; private set; } = false;

    [ViewVariables]
    public string? ServerName { get; private set; }

    [ViewVariables]
    private string? DiscordRoundEndRole { get; set; }

    private WebhookIdentifier? _webhookIdentifier;

    [ViewVariables]
    private string? RoundEndSoundCollection { get; set; }

#if EXCEPTION_TOLERANCE
        [ViewVariables]
        public int RoundStartFailShutdownCount { get; private set; } = 0;
#endif

    protected override void InitializeCVars()
    {
        base.InitializeCVars();

        Subs.CVar(Cfg, CCVars.ICRandomCharacters, e => _randomizeCharacters = e, true);
        Subs.CVar(Cfg,
            CCVars.GameLobbyEnabled,
            value =>
        {
            LobbyEnabled = value;
            foreach (var (userId, status) in _playerGameStatuses)
            {
                if (status == PlayerGameStatus.JoinedGame)
                    continue;
                _playerGameStatuses[userId] =
                    LobbyEnabled ? PlayerGameStatus.NotReadyToPlay : PlayerGameStatus.ReadyToPlay;
            }
        },
            true);
        Subs.CVar(Cfg, CCVars.GameDummyTicker, value => DummyTicker = value, true);
        Subs.CVar(Cfg, CCVars.GameLobbyDuration, value => LobbyDuration = TimeSpan.FromSeconds(value), true);
        Subs.CVar(Cfg,
            CCVars.GameDisallowLateJoins,
            value =>
            {
                DisallowLateJoin = value;
                UpdateLateJoinStatus();
            },
            true);
        Subs.CVar(Cfg,
            CCVars.AdminLogsServerName,
            value =>
        {
            // TODO why tf is the server name on admin logs
            ServerName = value;
        },
            true);
        Subs.CVar(Cfg,
            CCVars.DiscordRoundUpdateWebhook,
            value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _discord.GetWebhook(value, data => _webhookIdentifier = data.ToIdentifier());
            }
        },
            true);
        Subs.CVar(Cfg,
            CCVars.DiscordRoundEndRoleWebhook,
            value =>
        {
            DiscordRoundEndRole = value;

            if (value == string.Empty)
            {
                DiscordRoundEndRole = null;
            }
        },
            true);
        Subs.CVar(Cfg, CCVars.RoundEndSoundCollection, value => RoundEndSoundCollection = value, true);
#if EXCEPTION_TOLERANCE
            Subs.CVar(_cfg, CCVars.RoundStartFailShutdownCount, value => RoundStartFailShutdownCount = value, true);
#endif
    }
}
