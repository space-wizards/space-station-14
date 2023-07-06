using System.Linq;
using Content.Shared.Communications;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Communications.UI
{
    public sealed class CommunicationsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables] private CommunicationsConsoleMenu? _menu;

        public bool CanAnnounce { get; private set; }
        public bool CanCall { get; private set; }

        public bool CountdownStarted { get; private set; }
        public bool ErtCountdownStarted { get; private set; }
        public bool AlertLevelSelectable { get; private set; }
        private string CurrentLevel { get; set; } = default!;
        private string SelectedErtGroup { get; set; } = default!;
        public bool CanCallErt { get; private set; }

        public int Countdown => _expectedCountdownTime == null ? 0 : Math.Max((int)_expectedCountdownTime.Value.Subtract(_gameTiming.CurTime).TotalSeconds, 0);
        public float ErtCountdown => _expectedErtTime == null ? 0 : Math.Max((int)_expectedErtTime.Value.Subtract(_gameTiming.CurTime).TotalSeconds, 0);
        private TimeSpan? _expectedCountdownTime;
        private TimeSpan? _expectedErtTime;

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

        public void CallErtButtonPressed()
        {
            if (ErtCountdownStarted)
                RecallErt();
            else
                CallErt();
        }

        private void CallErt()
        {
            SendMessage(new CommunicationsConsoleCallErtMessage(SelectedErtGroup));
        }

        private void RecallErt()
        {
            SendMessage(new CommunicationsConsoleRecallErtMessage());
        }

        public void ErtGroupSelected(string level)
        {
            SelectedErtGroup = level;
            SendMessage(new CommunicationsConsoleSelectErtMessage());
        }

        public void AnnounceButtonPressed(string message)
        {
            var msg = (message.Length <= 256 ? message.Trim() : $"{message.Trim().Substring(0, 256)}...").ToCharArray();

            // No more than 2 newlines, other replaced to spaces
            var newlines = 0;
            for (var i = 0; i < msg.Length; i++)
            {
                if (msg[i] != '\n')
                    continue;

                if (newlines >= 2)
                    msg[i] = ' ';

                newlines++;
            }

            SendMessage(new CommunicationsConsoleAnnounceMessage(new string(msg)));
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
            CanCallErt = commsState.CanCallErt;
            _expectedCountdownTime = commsState.ExpectedCountdownEnd;
            _expectedErtTime = commsState.ErtCountdownTime;
            CountdownStarted = commsState.CountdownStarted;
            ErtCountdownStarted = commsState.ErtCountdownStarted;
            AlertLevelSelectable = commsState.AlertLevels != null && !float.IsNaN(commsState.CurrentAlertDelay) && commsState.CurrentAlertDelay <= 0;
            CurrentLevel = commsState.CurrentAlert;

            if (string.IsNullOrEmpty(SelectedErtGroup))
            {
                if (commsState.ErtsList != null && commsState.ErtsList.Count > 0)
                {
                    SelectedErtGroup = commsState.ErtsList[0];
                }
            }
            if (_menu != null)
            {
                _menu.UpdateCountdown();
                _menu.UpdateAlertLevels(commsState.AlertLevels, CurrentLevel);
                _menu.UpdateErtList(commsState.ErtsList, SelectedErtGroup);
                _menu.AlertLevelButton.Disabled = !AlertLevelSelectable;
                _menu.EmergencyShuttleButton.Disabled = !CanCall;
                _menu.AnnounceButton.Disabled = !CanAnnounce;
                _menu.CallErt.Disabled = !CanCallErt;
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
