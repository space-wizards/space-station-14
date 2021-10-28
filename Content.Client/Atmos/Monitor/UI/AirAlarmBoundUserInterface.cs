using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Monitor.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.Atmos.Monitor.UI
{
    public class AirAlarmBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private readonly AirAlarmDataSystem _airAlarmDataSystem;
        private AirAlarmWindow? _window;
        private EntityUid? _owner;

        public AirAlarmBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);

            _airAlarmDataSystem = EntitySystem.Get<AirAlarmDataSystem>();
        }

        protected override void Open()
        {
            base.Open();

            _window = new AirAlarmWindow();

            if (State != null) UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.AtmosDeviceDataChanged += OnDeviceDataChanged;
            _window.AtmosAlarmThresholdChanged += OnThresholdChanged;
            _window.AirAlarmModeChanged += OnAirAlarmModeChanged;
        }

        private void OnDeviceDataChanged(string address, IAtmosDeviceData data)
        {
            SendMessage(new AirAlarmUpdateDeviceDataMessage(address, data));
        }

        private void OnAirAlarmModeChanged(AirAlarmMode mode)
        {
            SendMessage(new AirAlarmUpdateAlarmModeMessage(mode));
        }

        private void OnThresholdChanged(AtmosMonitorThresholdType type, AtmosAlarmThreshold threshold, Gas? gas = null)
        {
            SendMessage(new AirAlarmUpdateAlarmThresholdMessage(type, threshold, gas));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            Logger.DebugS("AirAlarmUI", "Attempting to update data now.");

            if (_window == null
                || state is not AirAlarmBoundUserInterfaceState owner) return;

            _owner = owner.Uid;
            if (!_entityManager.TryGetComponent<AirAlarmDataComponent>(owner.Uid, out var data)) return;

            _window.UpdateState(data);
        }
    }
}
