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

        public int Countdown => _expectedCountdownTime == null
            ? 0 : Math.Max((int)_expectedCountdownTime.Value.Subtract(_gameTiming.CurTime).TotalSeconds, 0);
        private TimeSpan? _expectedCountdownTime;

        public CommunicationsConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new CommunicationsConsoleMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
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

            if (_menu != null)
            {
                _menu.UpdateCountdown();
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
