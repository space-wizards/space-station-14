using Content.Shared.Medical.CrewMonitoring;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Medical.CrewMonitoring
{
    public sealed class CrewMonitoringBoundUserInterface : BoundUserInterface
    {
        private CrewMonitoringWindow? _menu;

        public CrewMonitoringBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _menu = new CrewMonitoringWindow();
            _menu.OpenCentered();
            _menu.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case CrewMonitoringState st:
                    _menu?.ShowSensors(st.Sensors, st.WorldPosition, st.Snap, st.Precision);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
