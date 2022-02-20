using System;
using System.Linq;
using System.Threading;
using Robust.Shared.Enums;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        private static readonly TimeSpan UpdateRestartDelay = TimeSpan.FromSeconds(20);

        [ViewVariables]
        private bool _updateOnRoundEnd;
        private CancellationTokenSource? _updateShutdownCts;

        private void InitializeUpdates()
        {
            _watchdogApi.UpdateReceived += WatchdogApiOnUpdateReceived;
        }

        private void WatchdogApiOnUpdateReceived()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("game-ticker-restart-round-server-update"));
            _updateOnRoundEnd = true;
            ServerEmptyUpdateRestartCheck();
        }

        /// <summary>
        ///     Checks whether there are still players on the server,
        /// and if not starts a timer to automatically reboot the server if an update is available.
        /// </summary>
        private void ServerEmptyUpdateRestartCheck()
        {
            // Can't simple check the current connected player count since that doesn't update
            // before PlayerStatusChanged gets fired.
            // So in the disconnect handler we'd still see a single player otherwise.
            var playersOnline = _playerManager.Sessions.Any(p => p.Status != SessionStatus.Disconnected);
            if (playersOnline || !_updateOnRoundEnd)
            {
                // Still somebody online.
                return;
            }

            if (_updateShutdownCts is {IsCancellationRequested: false})
            {
                // Do nothing because I guess we already have a timer running..?
                return;
            }

            _updateShutdownCts = new CancellationTokenSource();

            Timer.Spawn(UpdateRestartDelay, () =>
            {
                _baseServer.Shutdown(Loc.GetString("game-ticker-shutdown-server-update"));
            }, _updateShutdownCts.Token);
        }
    }
}
