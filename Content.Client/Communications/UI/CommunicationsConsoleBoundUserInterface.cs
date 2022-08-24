using System;
using Content.Shared.Communications;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Communications.UI
{
    public sealed class CommunicationsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables] private CommunicationsConsoleMenu? _menu;

        public bool CanAnnounce { get; private set; }
        public bool CanCall { get; private set; }

        public bool CountdownStarted { get; private set; }

        public bool AlertLevelSelectable { get; private set; }

        public string CurrentLevel { get; private set; } = default!;

        public int Countdown => _expectedCountdownTime == null ? 0 : Math.Max((int)_expectedCountdownTime.Value.Subtract(_gameTiming.CurTime).TotalSeconds, 0);
        private TimeSpan? _expectedCountdownTime;

        public CommunicationsConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new CommunicationsConsoleMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void AlertLevelSelected(string level)
        {
            if (AlertLevelSelectable)
            {
                CurrentLevel = level;
                SendMessage(new CommunicationsConsoleSelectAlertLevelMessage(level));
            }
        }

        public void EmergencyShuttleButtonPressed()
        {
            if (CountdownStarted)
                RecallShuttle();
            else
                CallShuttle();
        }

        public void AnnounceButtonPressed(string message)
        {
            var msg = message.Length <= 256 ? message.Trim() : $"{message.Trim().Substring(0, 256)}...";
            SendMessage(new CommunicationsConsoleAnnounceMessage(msg));
        }

        public void CallShuttle()
        {
            SendMessage(new CommunicationsConsoleCallEmergencyShuttleMessage());
        }

        public void RecallShuttle()
        {
            SendMessage(new CommunicationsConsoleRecallEmergencyShuttleMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CommunicationsConsoleInterfaceState commsState)
                return;

            CanAnnounce = commsState.CanAnnounce;
            CanCall = commsState.CanCall;
            _expectedCountdownTime = commsState.ExpectedCountdownEnd;
            CountdownStarted = commsState.CountdownStarted;
            AlertLevelSelectable = commsState.AlertLevels != null && !float.IsNaN(commsState.CurrentAlertDelay) && commsState.CurrentAlertDelay <= 0;
            CurrentLevel = commsState.CurrentAlert;

            if (_menu != null)
            {
                _menu.UpdateCountdown();
                _menu.UpdateAlertLevels(commsState.AlertLevels, CurrentLevel);
                _menu.AlertLevelButton.Disabled = !AlertLevelSelectable;
                _menu.EmergencyShuttleButton.Disabled = !CanCall;
                _menu.AnnounceButton.Disabled = !CanAnnounce;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _menu?.Dispose();
        }
    }
}
