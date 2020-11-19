using System;
using Content.Client.Command;
using Content.Shared.GameObjects.Components.Command;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Command
{
    public class CommunicationsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables] private CommunicationsConsoleMenu _menu;

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
            if(CountdownStarted)
                RecallShuttle();
            else
                CallShuttle();
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

            _expectedCountdownTime = commsState.ExpectedCountdownEnd;
            CountdownStarted = commsState.CountdownStarted;
            _menu?.UpdateCountdown();

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _menu?.Dispose();
        }
    }
}
