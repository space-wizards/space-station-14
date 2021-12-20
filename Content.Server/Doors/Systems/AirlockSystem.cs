using Content.Server.Doors.Components;
using Content.Server.Power.Components;
using Content.Shared.Doors;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;

namespace Content.Server.Doors.Systems
{
    public class AirlockSystem : EntitySystem
    {
        [Dependency] private readonly DoorSystem _doorSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
            SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnActivate, before: new [] {typeof(DoorSystem)});
            SubscribeLocalEvent<AirlockComponent, BeforeDoorPryEvent>(OnDoorPry);
        }

        private void OnPowerChanged(EntityUid uid, AirlockComponent component, PowerChangedEvent args)
        {
            if (component.AppearanceComponent != null)
            {
                component.AppearanceComponent.SetData(DoorVisuals.Powered, args.Powered);
            }

            if (!args.Powered)
            {
                // stop any scheduled auto-closing
                _doorSystem.SetNextStateChange(uid, null);
            }
            else
            {
                // door received power. Lets "wake" the door up, in case it is currently open and needs to auto-close.
                _doorSystem.SetNextStateChange(uid, TimeSpan.FromSeconds(1));
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
                    ||  args.State != DoorState.Open;
            }
            // If the door is closed, we should look if the bolt was locked while closing
            component.UpdateBoltLightStatus();

            UpdateAutoClose(uid, component);
        }

        /// <summary>
        /// Updates the auto close timer.
        /// </summary>
        public void UpdateAutoClose(EntityUid uid, AirlockComponent? airlock = null, DoorComponent? door = null)
        {
            if (!Resolve(uid, ref airlock, ref door))
                return;

            if (door.State != DoorState.Open)
                return;

            if (!airlock.CanChangeState())
                return;

            var autoev = new BeforeDoorAutoCloseEvent();
            RaiseLocalEvent(uid, autoev, false);
            if (autoev.Cancelled)
                return;

            _doorSystem.SetNextStateChange(uid, airlock.AutoCloseDelay * airlock.AutoCloseDelayModifier);
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

        private void OnActivate(EntityUid uid, AirlockComponent component, ActivateInWorldEvent args)
        {
            if (component.WiresComponent != null && component.WiresComponent.IsPanelOpen &&
                TryComp(args.User, out ActorComponent? actor))
            {
                component.WiresComponent.OpenInterface(actor.PlayerSession);
                args.Handled = true;
            }
        }

        private void OnDoorPry(EntityUid uid, AirlockComponent component, BeforeDoorPryEvent args)
        {
            if (component.IsBolted())
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("airlock-component-cannot-pry-is-bolted-message"));
                args.Cancel();
            }
            if (component.IsPowered())
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("airlock-component-cannot-pry-is-powered-message"));
                args.Cancel();
            }
        }
    }
}
