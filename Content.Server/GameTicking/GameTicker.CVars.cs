using System;
using Content.Shared.CCVar;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        [ViewVariables]
        public bool LobbyEnabled { get; private set; } = false;

        [ViewVariables]
        public bool DummyTicker { get; private set; } = false;

        [ViewVariables]
        public TimeSpan LobbyDuration { get; private set; } = TimeSpan.Zero;

        [ViewVariables]
        public bool DisallowLateJoin { get; private set; } = false;

#if EXCEPTION_TOLERANCE
        [ViewVariables]
        public int RoundStartFailShutdownCount { get; private set; } = 0;
#endif

        private void InitializeCVars()
        {
            _configurationManager.OnValueChanged(CCVars.GameLobbyEnabled, value => LobbyEnabled = value, true);
            _configurationManager.OnValueChanged(CCVars.GameDummyTicker, value => DummyTicker = value, true);
            _configurationManager.OnValueChanged(CCVars.GameLobbyDuration, value => LobbyDuration = TimeSpan.FromSeconds(value), true);
            _configurationManager.OnValueChanged(CCVars.GameDisallowLateJoins,
                value => { DisallowLateJoin = value; UpdateLateJoinStatus(); UpdateJobsAvailable(); }, true);
#if EXCEPTION_TOLERANCE
            _configurationManager.OnValueChanged(CCVars.RoundStartFailShutdownCount, value => RoundStartFailShutdownCount = value, true);
#endif
        }
    }
}
