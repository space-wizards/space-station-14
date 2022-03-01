using Content.Server.Atmos.Monitor.Components;
using Content.Server.Power.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Interaction;
using Content.Shared.Emag.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos.Monitor.Systems
{
    public sealed class FireAlarmSystem : EntitySystem
    {
        [Dependency] private readonly AtmosMonitorSystem _monitorSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<FireAlarmComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<FireAlarmComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnInteractHand(EntityUid uid, FireAlarmComponent component, InteractHandEvent args)
        {
            if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
                return;

            if (EntityManager.TryGetComponent(args.User, out ActorComponent? actor)
                && EntityManager.TryGetComponent(uid, out AtmosMonitorComponent? monitor)
                && EntityManager.TryGetComponent(uid, out ApcPowerReceiverComponent? power)
                && power.Powered)
            {
                if (monitor.HighestAlarmInNetwork == AtmosMonitorAlarmType.Normal)
                {
                    _monitorSystem.Alert(uid, AtmosMonitorAlarmType.Danger);
                }
                else
                {
                    _monitorSystem.ResetAll(uid);
                }
            }
        }

        private void OnEmagged(EntityUid uid, FireAlarmComponent component, GotEmaggedEvent args)
        {
            if (TryComp<AtmosMonitorComponent>(uid, out var atmosMonitor))
            {
                if (atmosMonitor?.MonitorFire == true)
                {
                    atmosMonitor.MonitorFire = false;
                    _monitorSystem.Alert(uid, AtmosMonitorAlarmType.Emagged);
                    args.Handled = true;
                }
            }
        }
    }
}
