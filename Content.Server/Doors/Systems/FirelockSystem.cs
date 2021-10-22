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

            SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
            SubscribeLocalEvent<FirelockComponent, DoorGetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
            SubscribeLocalEvent<FirelockComponent, DoorClickShouldActivateEvent>(OnDoorClickShouldActivate);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorPryEvent>(OnBeforeDoorPry);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorAutoCloseEvent>(OnBeforeDoorAutoclose);
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

        private void OnDoorClickShouldActivate(EntityUid uid, FirelockComponent component, DoorClickShouldActivateEvent args)
        {
            // We're a firelock, you can't click to open it
            args.Handled = true;
        }

        private void OnBeforeDoorPry(EntityUid uid, FirelockComponent component, BeforeDoorPryEvent args)
        {
            if (component.DoorComponent == null || component.DoorComponent.State != SharedDoorComponent.DoorState.Closed)
            {
                return;
            }

            if (component.IsHoldingPressure())
            {
                component.Owner.PopupMessage(args.Args.User, Loc.GetString("firelock-component-is-holding-pressure-message"));
            }
            else if (component.IsHoldingFire())
            {
                component.Owner.PopupMessage(args.Args.User, Loc.GetString("firelock-component-is-holding-fire-message"));
            }
        }

        private void OnBeforeDoorAutoclose(EntityUid uid, FirelockComponent component, BeforeDoorAutoCloseEvent args)
        {
            // Firelocks can't autoclose, they must be manually closed
            args.Cancel();
        }
    }
}
