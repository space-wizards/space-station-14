using Content.Server.Administration.Managers;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Afk
{
    /// <summary>
    /// Tracks AFK (away from keyboard) status for players.
    /// </summary>
    /// <seealso cref="CCVars.AfkTime"/>
    public interface IAfkManager
    {
        /// <summary>
        /// Raised whenever <see cref="PlayerDidAction"/> records activity for a player.
        /// </summary>
        event Action<ICommonSession> PlayerDidActionEvent;

        /// <summary>
        /// Check whether this player is currently AFK.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player is AFK, false otherwise.</returns>
        bool IsAfk(ICommonSession player);

        /// <summary>
        /// Resets AFK status for the player as if they just did an action and are definitely not AFK.
        /// </summary>
        /// <param name="player">The player to set AFK status for.</param>
        void PlayerDidAction(ICommonSession player);

        /// <summary>
        /// Resets AFK status for the player as if they just did an action and are definitely not AFK.
        /// </summary>
        /// <param name="channel">The player's network channel.</param>
        void PlayerDidAction(INetChannel channel);

        void Initialize();
    }

    [UsedImplicitly]
    public sealed partial class AfkManager : IAfkManager
    {
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private IConfigurationManager _cfg = default!;
        [Dependency] private IConsoleHost _consoleHost = default!;
        [Dependency] private IAdminManager _adminManager = default!;
        [Dependency] private ILogManager _logManager = default!;

        private readonly Dictionary<ICommonSession, TimeSpan> _lastActionTimes = new();
        private ISawmill _sawmill = default!;

        private TimeSpan _adminAfkTime;
        private TimeSpan _afkTime;

        public event Action<ICommonSession>? PlayerDidActionEvent;

        public void Initialize()
        {
            // Connecting, console commands and input commands all reset AFK status.

            _sawmill = _logManager.GetSawmill("afk");
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
            _consoleHost.AnyCommandExecuted += ConsoleHostOnAnyCommandExecuted;
            _cfg.OnValueChanged(CCVars.AfkTime, value => _afkTime = TimeSpan.FromSeconds(value), true);
            _cfg.OnValueChanged(CCVars.AdminAfkTime, value => _adminAfkTime = TimeSpan.FromSeconds(value), true);
        }

        public void PlayerDidAction(ICommonSession player)
        {
            if (player.Status == SessionStatus.Disconnected)
                // Make sure we don't re-add to the dictionary if the player is disconnected now.
                return;

            _lastActionTimes[player] = _gameTiming.RealTime;
            _sawmill.Verbose($"Reset AFK timer for {player.Name} ({player.UserId}).");
            PlayerDidActionEvent?.Invoke(player);
        }

        public void PlayerDidAction(INetChannel channel)
        {
            if (_playerManager.TryGetSessionByChannel(channel, out var session))
                PlayerDidAction(session);
        }

        public bool IsAfk(ICommonSession player)
        {
            if (!_lastActionTimes.TryGetValue(player, out var time))
            {
                // Some weird edge case like disconnected clients. Just say true I guess.
                return true;
            }

            TimeSpan timeOut;

            if (_adminManager.IsAdmin(player))
            {
                timeOut = _adminAfkTime;
            }
            else
            {
                timeOut = _afkTime;
            }

            return _gameTiming.RealTime - time > timeOut;
        }

        private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Disconnected)
            {
                _lastActionTimes.Remove(e.Session);
                return;
            }

            PlayerDidAction(e.Session);
        }

        private void ConsoleHostOnAnyCommandExecuted(IConsoleShell shell, string commandname, string argstr, string[] args)
        {
            if (shell.Player is { } player)
                PlayerDidAction(player);
        }
    }
}
