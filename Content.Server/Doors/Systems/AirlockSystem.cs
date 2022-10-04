using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Doors.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Tools.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server.Doors.Systems
{
    public sealed class AirlockSystem : SharedAirlockSystem
    {
        [Dependency] private readonly WiresSystem _wiresSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;

        /// <summary>
        ///     Toggles an airlock open and closed.
        /// </summary>
        public const string AirlockToggleOpen = "airlock_toggle_open";

        /// <summary>
        ///     Toggles the bolts on an airlock.
        /// </summary>
        public const string AirlockToggleBolts = "airlock_toggle_bolts";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
            SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnActivate, before: new [] {typeof(DoorSystem)});
            SubscribeLocalEvent<AirlockComponent, DoorGetPryTimeModifierEvent>(OnGetPryMod);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorPryEvent>(OnDoorPry);
            SubscribeLocalEvent<AirlockComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        }

        private void OnPacketReceived(EntityUid uid, AirlockComponent component, DeviceNetworkPacketEvent args)
        {
            if (!TryComp<DoorComponent>(uid, out var door))
            {
                return;
            }

            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmdObj)
                || cmdObj is not string cmd
                || cmd != DeviceNetworkConstants.CmdSetState)
            {
                return;
            }

            if (!args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out var stateObj)
                || stateObj is not AirlockStatusToggle state)
            {
                return;
            }

            switch (state)
            {
                case AirlockStatusToggle.Open:
                    switch (door.State)
                    {
                        case DoorState.Closed:
                            DoorSystem.TryOpen(uid, door);
                            break;
                        case DoorState.Open:
                            DoorSystem.TryClose(uid, door);
                            break;
                    }

                    break;
                case AirlockStatusToggle.Bolts:
                    component.SetBoltsWithAudio(!component.BoltsDown);
                    break;
            }
        }

        private void OnPowerChanged(EntityUid uid, AirlockComponent component, PowerChangedEvent args)
        {
            if (TryComp<AppearanceComponent>(uid, out var appearanceComponent))
            {
                appearanceComponent.SetData(DoorVisuals.Powered, args.Powered);
            }

            if (!TryComp(uid, out DoorComponent? door))
                return;

            if (!args.Powered)
            {
                // stop any scheduled auto-closing
                if (door.State == DoorState.Open)
                    DoorSystem.SetNextStateChange(uid, null);
            }
            else
            {
                UpdateAutoClose(uid, door: door);
            }

            // BoltLights also got out
            component.UpdateBoltLightStatus();
        }

        private void OnStateChanged(EntityUid uid, AirlockComponent component, DoorStateChangedEvent args)
        {
            // TODO move to shared? having this be server-side, but having client-side door opening/closing & prediction
            // means that sometimes the panels & bolt lights may be visible despite a door being completely open.

            // Only show the maintenance panel if the airlock is closed
            if (TryComp<WiresComponent>(uid, out var wiresComponent))
            {
                wiresComponent.IsPanelVisible =
                    component.OpenPanelVisible
                    ||  args.State != DoorState.Open;
            }
            // If the door is closed, we should look if the bolt was locked while closing
            component.UpdateBoltLightStatus();

            UpdateAutoClose(uid, component);

            // Make sure the airlock auto closes again next time it is opened
            if (args.State == DoorState.Closed)
                component.AutoClose = true;
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

            if (!airlock.AutoClose)
                return;

            if (!airlock.CanChangeState())
                return;

            var autoev = new BeforeDoorAutoCloseEvent();
            RaiseLocalEvent(uid, autoev, false);
            if (autoev.Cancelled)
                return;

            DoorSystem.SetNextStateChange(uid, airlock.AutoCloseDelay * airlock.AutoCloseDelayModifier);
        }

        private void OnBeforeDoorOpened(EntityUid uid, AirlockComponent component, BeforeDoorOpenedEvent args)
        {
            if (!component.CanChangeState())
                args.Cancel();
        }

        protected override void OnBeforeDoorClosed(EntityUid uid, SharedAirlockComponent component, BeforeDoorClosedEvent args)
        {
            base.OnBeforeDoorClosed(uid, component, args);

            if (args.Cancelled)
                return;

            // only block based on bolts / power status when initially closing the door, not when its already
            // mid-transition. Particularly relevant for when the door was pried-closed with a crowbar, which bypasses
            // the initial power-check.

            if (TryComp(uid, out DoorComponent? door)
                && !door.Partial
                && !Comp<AirlockComponent>(uid).CanChangeState())
            {
                args.Cancel();
            }
        }

        private void OnBeforeDoorDenied(EntityUid uid, AirlockComponent component, BeforeDoorDeniedEvent args)
        {
            if (!component.CanChangeState())
                args.Cancel();
        }

        private void OnActivate(EntityUid uid, AirlockComponent component, ActivateInWorldEvent args)
        {
            if (TryComp<WiresComponent>(uid, out var wiresComponent) && wiresComponent.IsPanelOpen &&
                EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            {
                _wiresSystem.OpenUserInterface(uid, actor.PlayerSession);
                args.Handled = true;
                return;
            }

            if (component.KeepOpenIfClicked)
            {
                // Disable auto close
                component.AutoClose = false;
            }
        }

        private void OnGetPryMod(EntityUid uid, AirlockComponent component, DoorGetPryTimeModifierEvent args)
        {
            if (_power.IsPowered(uid))
                args.PryTimeModifier *= component.PoweredPryModifier;
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
                if (HasComp<ToolForcePoweredComponent>(args.Tool))
                    return;
                component.Owner.PopupMessage(args.User, Loc.GetString("airlock-component-cannot-pry-is-powered-message"));
                args.Cancel();
            }
        }
    }

    public enum AirlockStatusToggle
    {
        Open,
        Bolts
    }
}
