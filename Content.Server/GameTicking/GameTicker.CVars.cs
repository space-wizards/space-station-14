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
        public string ChosenMap { get; private set; } = string.Empty;

        [ViewVariables]
        public TimeSpan LobbyDuration { get; private set; } = TimeSpan.Zero;

        [ViewVariables]
        public bool DisallowLateJoin { get; private set; } = false;

        private void InitializeCVars()
        {
            _configurationManager.OnValueChanged(CCVars.GameLobbyEnabled, value => LobbyEnabled = value, true);
            _configurationManager.OnValueChanged(CCVars.GameDummyTicker, value => DummyTicker = value, true);
            _configurationManager.OnValueChanged(CCVars.GameMap, value => ChosenMap = value, true);
            _configurationManager.OnValueChanged(CCVars.GameLobbyDuration, value => LobbyDuration = TimeSpan.FromSeconds(value), true);
            _configurationManager.OnValueChanged(CCVars.GameDisallowLateJoins, invokeImmediately:true,
                onValueChanged:value => { DisallowLateJoin = value; UpdateLateJoinStatus(); UpdateJobsAvailable(); });
        }
    }
}
