using Content.Server.DeviceLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Tools.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.Wires;
using Content.Server.MachineLinking.System;

namespace Content.Server.Doors.Systems
{
    public sealed class AirlockSystem : SharedAirlockSystem
    {
        [Dependency] private readonly WiresSystem _wiresSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AirlockComponent, ComponentInit>(OnAirlockInit);
            SubscribeLocalEvent<AirlockComponent, SignalReceivedEvent>(OnSignalReceived);

            SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
            SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnActivate, before: new [] {typeof(DoorSystem)});
            SubscribeLocalEvent<AirlockComponent, DoorGetPryTimeModifierEvent>(OnGetPryMod);
            SubscribeLocalEvent<AirlockComponent, BeforeDoorPryEvent>(OnDoorPry);

        }

        private void OnAirlockInit(EntityUid uid, AirlockComponent component, ComponentInit args)
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var receiverComponent))
            {
                Appearance.SetData(uid, DoorVisuals.Powered, receiverComponent.Powered);
            }
        }

        private void OnSignalReceived(EntityUid uid, AirlockComponent component, ref SignalReceivedEvent args)
        {
            if (args.Port == component.AutoClosePort)
            {
                component.AutoClose = false;
            }
        }

        private void OnPowerChanged(EntityUid uid, AirlockComponent component, ref PowerChangedEvent args)
        {
            if (TryComp<AppearanceComponent>(uid, out var appearanceComponent))
            {
                Appearance.SetData(uid, DoorVisuals.Powered, args.Powered, appearanceComponent);
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
                if (component.BoltWireCut)
                    SetBoltsWithAudio(uid, component, true);

                UpdateAutoClose(uid, door: door);
            }

            // BoltLights also got out
            UpdateBoltLightStatus(uid, component);
        }

        private void OnStateChanged(EntityUid uid, AirlockComponent component, DoorStateChangedEvent args)
        {
            // TODO move to shared? having this be server-side, but having client-side door opening/closing & prediction
            // means that sometimes the panels & bolt lights may be visible despite a door being completely open.

            // Only show the maintenance panel if the airlock is closed
            if (TryComp<WiresPanelComponent>(uid, out var wiresPanel))
            {
                _wiresSystem.ChangePanelVisibility(uid, wiresPanel, component.OpenPanelVisible || args.State != DoorState.Open);
            }
            // If the door is closed, we should look if the bolt was locked while closing
            UpdateBoltLightStatus(uid, component);
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

            if (!CanChangeState(uid, airlock))
                return;

            var autoev = new BeforeDoorAutoCloseEvent();
            RaiseLocalEvent(uid, autoev, false);
            if (autoev.Cancelled)
                return;

            DoorSystem.SetNextStateChange(uid, airlock.AutoCloseDelay * airlock.AutoCloseDelayModifier);
        }

        private void OnBeforeDoorOpened(EntityUid uid, AirlockComponent component, BeforeDoorOpenedEvent args)
        {
            if (!CanChangeState(uid, component))
                args.Cancel();
        }

        protected override void OnBeforeDoorClosed(EntityUid uid, AirlockComponent component, BeforeDoorClosedEvent args)
        {
            base.OnBeforeDoorClosed(uid, component, args);

            if (args.Cancelled)
                return;

            // only block based on bolts / power status when initially closing the door, not when its already
            // mid-transition. Particularly relevant for when the door was pried-closed with a crowbar, which bypasses
            // the initial power-check.

            if (TryComp(uid, out DoorComponent? door)
                && !door.Partial
                && !CanChangeState(uid, component))
            {
                args.Cancel();
            }
        }

        private void OnBeforeDoorDenied(EntityUid uid, AirlockComponent component, BeforeDoorDeniedEvent args)
        {
            if (!CanChangeState(uid, component))
                args.Cancel();
        }

        private void OnActivate(EntityUid uid, AirlockComponent component, ActivateInWorldEvent args)
        {
            if (TryComp<WiresPanelComponent>(uid, out var panel) && panel.Open &&
                TryComp<ActorComponent>(args.User, out var actor))
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
            if (component.BoltsDown)
            {
                Popup.PopupEntity(Loc.GetString("airlock-component-cannot-pry-is-bolted-message"), uid, args.User);
                args.Cancel();
            }

            if (this.IsPowered(uid, EntityManager))
            {
                if (HasComp<ToolForcePoweredComponent>(args.Tool))
                    return;
                Popup.PopupEntity(Loc.GetString("airlock-component-cannot-pry-is-powered-message"), uid, args.User);
                args.Cancel();
            }
        }

        public bool CanChangeState(EntityUid uid, AirlockComponent component)
        {
            return this.IsPowered(uid, EntityManager) && !component.BoltsDown;
        }

        public void UpdateBoltLightStatus(EntityUid uid, AirlockComponent component)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            Appearance.SetData(uid, DoorVisuals.BoltLights, GetBoltLightsVisible(uid, component), appearance);
        }

        public void SetBoltsWithAudio(EntityUid uid, AirlockComponent component, bool newBolts)
        {
            if (newBolts == component.BoltsDown)
                return;

            component.BoltsDown = newBolts;
            Audio.PlayPvs(newBolts ? component.BoltDownSound : component.BoltUpSound, uid);
            UpdateBoltLightStatus(uid, component);
        }

        public bool GetBoltLightsVisible(EntityUid uid, AirlockComponent component)
        {
            return component.BoltLightsEnabled &&
                   component.BoltsDown &&
                   this.IsPowered(uid, EntityManager) &&
                   TryComp<DoorComponent>(uid, out var doorComponent) &&
                   doorComponent.State == DoorState.Closed;
        }

        public void SetBoltLightsEnabled(EntityUid uid, AirlockComponent component, bool value)
        {
            if (component.BoltLightsEnabled == value)
                return;

            component.BoltLightsEnabled = value;
            UpdateBoltLightStatus(uid, component);
        }

        public void SetBoltsDown(EntityUid uid, AirlockComponent component, bool value)
        {
            if (component.BoltsDown == value)
                return;

            component.BoltsDown = value;
            UpdateBoltLightStatus(uid, component);
        }
    }
}
