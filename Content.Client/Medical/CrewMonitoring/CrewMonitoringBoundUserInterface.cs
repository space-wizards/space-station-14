using Content.Shared.Medical.CrewMonitoring;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Medical.CrewMonitoring
{
    public sealed class CrewMonitoringBoundUserInterface : BoundUserInterface
    {
        private readonly IEntityManager _entManager;
        private CrewMonitoringWindow? _menu;

        public CrewMonitoringBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            _entManager = IoCManager.Resolve<IEntityManager>();
        }

        protected override void Open()
        {
            EntityUid? gridUid = null;

            if (_entManager.TryGetComponent<TransformComponent>(Owner.Owner, out var xform))
            {
                gridUid = xform.GridUid;
            }

            _menu = new CrewMonitoringWindow(gridUid);

            _menu.OpenCentered();
            _menu.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case CrewMonitoringState st:
                    _entManager.TryGetComponent<TransformComponent>(Owner.Owner, out var xform);

                    _menu?.ShowSensors(st.Sensors, xform?.Coordinates, st.Snap, st.Precision);
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
