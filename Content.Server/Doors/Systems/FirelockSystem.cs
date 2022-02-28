using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Doors.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Doors.Systems
{
    public sealed class FirelockSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoorSystem _doorSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
            SubscribeLocalEvent<FirelockComponent, DoorGetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorPryEvent>(OnBeforeDoorPry);

            SubscribeLocalEvent<FirelockComponent, BeforeDoorAutoCloseEvent>(OnBeforeDoorAutoclose);
            SubscribeLocalEvent<FirelockComponent, AtmosMonitorAlarmEvent>(OnAtmosAlarm);
        }

        private void OnBeforeDoorOpened(EntityUid uid, FirelockComponent component, BeforeDoorOpenedEvent args)
        {
            if (component.IsHoldingFire() || component.IsHoldingPressure())
                args.Cancel();
        }

        private void OnBeforeDoorDenied(EntityUid uid, FirelockComponent component, BeforeDoorDeniedEvent args)
        {
            args.Cancel();
        }

        private void OnDoorGetPryTimeModifier(EntityUid uid, FirelockComponent component, DoorGetPryTimeModifierEvent args)
        {
            if (component.IsHoldingFire() || component.IsHoldingPressure())
                args.PryTimeModifier *= component.LockedPryTimeModifier;
        }

        private void OnBeforeDoorPry(EntityUid uid, FirelockComponent component, BeforeDoorPryEvent args)
        {
            if (!TryComp<DoorComponent>(uid, out var door) || door.State != DoorState.Closed)
            {
                return;
            }

            if (component.IsHoldingPressure())
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("firelock-component-is-holding-pressure-message"));
            }
            else if (component.IsHoldingFire())
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("firelock-component-is-holding-fire-message"));
            }
        }

        private void OnBeforeDoorAutoclose(EntityUid uid, FirelockComponent component, BeforeDoorAutoCloseEvent args)
        {
            // Make firelocks autoclose, but only if the last alarm type it
            // remembers was a danger. This is to prevent people from
            // flooding hallways with endless bad air/fire.
            if (!EntityManager.TryGetComponent(uid, out AtmosAlarmableComponent alarmable))
            {
                args.Cancel();
                return;
            }
            if (alarmable.HighestNetworkState != AtmosMonitorAlarmType.Danger)
                args.Cancel();
        }

        private void OnAtmosAlarm(EntityUid uid, FirelockComponent component, AtmosMonitorAlarmEvent args)
        {
            if (!TryComp<DoorComponent>(uid, out var doorComponent)) return;

            if (args.HighestNetworkType == AtmosMonitorAlarmType.Normal)
            {
                if (doorComponent.State == DoorState.Closed)
                    _doorSystem.TryOpen(uid);
            }
            else if (args.HighestNetworkType == AtmosMonitorAlarmType.Danger)
            {
                component.EmergencyPressureStop();
            }
        }
    }
}
