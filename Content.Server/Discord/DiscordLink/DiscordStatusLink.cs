using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.CCVar;
using NetCord.Gateway;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Discord.DiscordLink;

public sealed class DiscordStatusLink
{
    public const string PlayerStatusId = "PlayerStatus";

    [Dependency] private readonly DiscordLink _discordLink = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _entMan = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private StatusRef _statusRef = null!;
    private DateTime _nextUpdate;

    public void Update()
    {
        if (DateTime.Now < _nextUpdate)
            return;

        UpdateStatus();
        _nextUpdate = DateTime.Now.AddSeconds(1);
    }

    public void Initialize()
    {
        _statusRef = _discordLink.GetOrCreateStatusRef(PlayerStatusId);
        _statusRef.Type = UserActivityType.Playing;

        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
    }

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        var players = _cfg.GetCVar(CCVars.AdminsCountInReportedPlayerCount)
            ? _playerManager.PlayerCount
            : _playerManager.PlayerCount - _adminManager.ActiveAdmins.Count();
        var maxPlayers = _cfg.GetCVar(CCVars.SoftMaxPlayers);

        var gameTicker = _entMan.GetEntitySystem<GameTicker>();

        if (gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
        {
            if (players == 0)
            {
                _statusRef.Message = _localizationManager.GetString("discord-status-lobby-alone",
                    ("maxPlayers", maxPlayers));
            }
            else if (gameTicker.Paused)
            {
                _statusRef.Message = _localizationManager.GetString("discord-status-lobby-paused",
                    ("players", players),
                    ("maxPlayers", maxPlayers));
            }
            else
            {
                var difference = gameTicker.RoundStartTime - _timing.CurTime;

                _statusRef.Message = _localizationManager.GetString("discord-status-lobby",
                    ("players", players),
                    ("maxPlayers", maxPlayers),
                    ("timeLeft", difference.ToString(@"mm\:ss")));
            }
        }
        else
        {
            _statusRef.Message = _localizationManager.GetString("discord-status-ingame",
                ("players", players),
                ("maxPlayers", maxPlayers),
                ("map", _gameMapManager.GetSelectedMap()?.MapName!),
                ("elapsed", _timing.CurTime.Subtract(gameTicker.RoundStartTimeSpan).ToString(@"hh\:mm\:ss")));
        }
    }
}
