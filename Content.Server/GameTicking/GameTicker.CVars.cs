using System;
using Content.Shared.CCVar;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        [ViewVariables]
        public bool LobbyEnabled { get; private set; } = false;

        [ViewVariables]
        public bool DummyTicker { get; private set; } = false;

        [ViewVariables]
        public TimeSpan LobbyDuration { get; private set; } = TimeSpan.Zero;

        [ViewVariables]
        public bool DisallowLateJoin { get; private set; } = false;

        [ViewVariables]
        public bool StationOffset { get; private set; } = false;

        [ViewVariables]
        public bool StationRotation { get; private set; } = false;

        [ViewVariables]
        public float MaxStationOffset { get; private set; } = 0f;

        private void InitializeCVars()
        {
            _configurationManager.OnValueChanged(CCVars.GameLobbyEnabled, value => LobbyEnabled = value, true);
            _configurationManager.OnValueChanged(CCVars.GameDummyTicker, value => DummyTicker = value, true);
            _configurationManager.OnValueChanged(CCVars.GameLobbyDuration, value => LobbyDuration = TimeSpan.FromSeconds(value), true);
            _configurationManager.OnValueChanged(CCVars.GameDisallowLateJoins,
                value => { DisallowLateJoin = value; UpdateLateJoinStatus(); UpdateJobsAvailable(); }, true);
            _configurationManager.OnValueChanged(CCVars.StationOffset, value => StationOffset = value, true);
            _configurationManager.OnValueChanged(CCVars.StationRotation, value => StationRotation = value, true);
            _configurationManager.OnValueChanged(CCVars.MaxStationOffset, value => MaxStationOffset = value, true);
        }
    }
}
