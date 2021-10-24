using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.Atmos.Monitor;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public class FireAlarmComponent : Component, IInteractHand
    {
        [ComponentDependency] private readonly AtmosMonitorComponent? _monitorComponent = default!;
        [ComponentDependency] private readonly ApcPowerReceiverComponent? _powerComponent = default!;
        private AtmosMonitorSystem _monitorSystem = default!;
        public override string Name => "FireAlarm";

        protected override void Initialize()
        {
            base.Initialize();

            _monitorSystem = EntitySystem.Get<AtmosMonitorSystem>();
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out ActorComponent? actor)
                && _monitorComponent != null
                && _powerComponent != null
                && _powerComponent.Powered)
            {
                if (_monitorComponent.HighestAlarmInNetwork() == AtmosMonitorAlarmType.Normal)
                {
                    _monitorSystem.Alert(_monitorComponent, AtmosMonitorAlarmType.Danger);
                }
                else
                {
                    _monitorSystem.ResetAll(_monitorComponent.Owner.Uid);
                }
                return true;
            }

            return false;
        }
    }
}
