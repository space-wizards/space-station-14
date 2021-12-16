using Content.Server.Doors.Components;
using Content.Shared.Doors;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Doors.Systems
{
    public class FirelockSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FirelockComponent, DoorOpenAttemptEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
            SubscribeLocalEvent<FirelockComponent, DoorGetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorPryEvent>(OnBeforeDoorPry);
        }

        private void OnBeforeDoorOpened(EntityUid uid, FirelockComponent component, DoorOpenAttemptEvent args)
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
            if (component.DoorComponent == null || component.DoorComponent.State != DoorState.Closed)
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
    }
}
