using Content.Server.Doors.Components;
using Content.Server.Power.Components;
using Content.Shared.Doors;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Doors.Systems
{
    public class AirlockSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
            SubscribeLocalEvent<AirlockComponent, DoorSafetyEnabledEvent>(OnDoorSafetyCheck);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorAutoCloseEvent>(OnDoorAutoCloseCheck);
            SubscribeLocalEvent<AirlockComponent, DoorGetCloseTimeModifierEvent>(OnDoorCloseTimeModifier);
            SubscribeLocalEvent<AirlockComponent, DoorClickShouldActivateEvent>(OnDoorClickShouldActivate);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorPryEvent>(OnDoorPry);
        }

        private void OnPowerChanged(EntityUid uid, AirlockComponent component, PowerChangedEvent args)
        {
            if (component.AppearanceComponent != null)
            {
                component.AppearanceComponent.SetData(DoorVisuals.Powered, args.Powered);
            }

            // BoltLights also got out
            component.UpdateBoltLightStatus();
        }

        private void OnStateChanged(EntityUid uid, AirlockComponent component, DoorStateChangedEvent args)
        {
            // Only show the maintenance panel if the airlock is closed
            if (component.WiresComponent != null)
            {
                component.WiresComponent.IsPanelVisible =
                    component.OpenPanelVisible
                    ||  args.State != SharedDoorComponent.DoorState.Open;
            }
            // If the door is closed, we should look if the bolt was locked while closing
            component.UpdateBoltLightStatus();
        }

        private void OnBeforeDoorOpened(EntityUid uid, AirlockComponent component, BeforeDoorOpenedEvent args)
        {
            if (!component.CanChangeState())
                args.Cancel();
        }

        private void OnBeforeDoorClosed(EntityUid uid, AirlockComponent component, BeforeDoorClosedEvent args)
        {
            if (!component.CanChangeState())
                args.Cancel();
        }

        private void OnBeforeDoorDenied(EntityUid uid, AirlockComponent component, BeforeDoorDeniedEvent args)
        {
            if (!component.CanChangeState())
                args.Cancel();
        }

        private void OnDoorSafetyCheck(EntityUid uid, AirlockComponent component, DoorSafetyEnabledEvent args)
        {
            args.Safety = component.Safety;
        }

        private void OnDoorAutoCloseCheck(EntityUid uid, AirlockComponent component, BeforeDoorAutoCloseEvent args)
        {
            if (!component.AutoClose)
                args.Cancel();
        }

        private void OnDoorCloseTimeModifier(EntityUid uid, AirlockComponent component, DoorGetCloseTimeModifierEvent args)
        {
            args.CloseTimeModifier *= component.AutoCloseDelayModifier;
        }

        private void OnDoorClickShouldActivate(EntityUid uid, AirlockComponent component, DoorClickShouldActivateEvent args)
        {
            if (component.WiresComponent != null && component.WiresComponent.IsPanelOpen &&
                args.Args.User.TryGetComponent(out ActorComponent? actor))
            {
                component.WiresComponent.OpenInterface(actor.PlayerSession);
                args.Handled = true;
            }
        }

        private void OnDoorPry(EntityUid uid, AirlockComponent component, BeforeDoorPryEvent args)
        {
            if (component.IsBolted())
            {
                component.Owner.PopupMessage(args.Args.User, Loc.GetString("airlock-component-cannot-pry-is-bolted-message"));
                args.Cancel();
            }
            if (component.IsPowered())
            {
                component.Owner.PopupMessage(args.Args.User, Loc.GetString("airlock-component-cannot-pry-is-powered-message"));
                args.Cancel();
            }
        }
    }
}
