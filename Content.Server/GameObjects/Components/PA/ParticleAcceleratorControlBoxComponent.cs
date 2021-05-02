#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameObjects.Components.PA
{
    // This component is in control of the PA's logic because it's the one to contain the wires for hacking.
    // And also it's the only PA component that meaningfully needs to work on its own.
    /// <summary>
    ///     Is the computer thing people interact with to control the PA.
    ///     Also contains primary logic for actual PA behavior, part scanning, etc...
    /// </summary>
    [ComponentReference(typeof(IActivate))]
    [RegisterComponent]
    public class ParticleAcceleratorControlBoxComponent : ParticleAcceleratorPartComponent, IActivate, IWires
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override string Name => "ParticleAcceleratorControlBox";

        [ViewVariables]
        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ParticleAcceleratorControlBoxUiKey.Key);

        /// <summary>
        ///     Power receiver for the control console itself.
        /// </summary>
        [ViewVariables] private PowerReceiverComponent _powerReceiverComponent = default!;

        [ViewVariables] private ParticleAcceleratorFuelChamberComponent? _partFuelChamber;
        [ViewVariables] private ParticleAcceleratorEndCapComponent? _partEndCap;
        [ViewVariables] private ParticleAcceleratorPowerBoxComponent? _partPowerBox;
        [ViewVariables] private ParticleAcceleratorEmitterComponent? _partEmitterLeft;
        [ViewVariables] private ParticleAcceleratorEmitterComponent? _partEmitterCenter;
        [ViewVariables] private ParticleAcceleratorEmitterComponent? _partEmitterRight;
        [ViewVariables] private ParticleAcceleratorPowerState _selectedStrength = ParticleAcceleratorPowerState.Standby;

        [ViewVariables] private bool _isAssembled;

        // Enabled: power switch is on
        [ViewVariables] private bool _isEnabled;

        // Powered: power switch is on AND the PA is actively firing (if not on standby)
        [ViewVariables] private bool _isPowered;
        [ViewVariables] private bool _wireInterfaceBlocked;
        [ViewVariables] private bool _wirePowerBlocked;
        [ViewVariables] private bool _wireLimiterCut;
        [ViewVariables] private bool _wireStrengthCut;
        [ViewVariables] private CancellationTokenSource? _fireCancelTokenSrc;

        /// <summary>
        ///     Delay between consecutive PA shots.
        /// </summary>
        // Fun fact:
        // On /vg/station (can't check TG because lol they removed singulo),
        // the PA emitter parts have var/fire_delay = 50.
        // For anybody from the future not BYOND-initiated, that's 5 seconds.
        // However, /obj/machinery/particle_accelerator/control_box/process(),
        // which calls emit_particle() on the emitters,
        // only gets called every *2* seconds, because of CarnMC timing.
        // So the *actual* effective firing delay of the PA is 6 seconds, not 5 as listed in the code.
        // So...
        // I have reflected that here to be authentic.
        [ViewVariables(VVAccess.ReadWrite)] [DataField("fireDelay")] private TimeSpan _firingDelay = TimeSpan.FromSeconds(6);

        [ViewVariables(VVAccess.ReadWrite)] [DataField("powerDrawBase")] private int _powerDrawBase = 500;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("powerDrawMult")] private int _powerDrawMult = 1500;

        [ViewVariables] private bool ConsolePowered => _powerReceiverComponent?.Powered ?? true;

        public ParticleAcceleratorControlBoxComponent()
        {
            Master = this;
        }

        private ParticleAcceleratorPowerState MaxPower => _wireLimiterCut
            ? ParticleAcceleratorPowerState.Level3
            : ParticleAcceleratorPowerState.Level2;

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            Owner.EnsureComponent(out _powerReceiverComponent);

            _powerReceiverComponent!.Load = 250;
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    OnPowerStateChanged(powerChanged);
                    break;
            }
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateWireStatus();
        }

        // This is the power state for the PA control box itself.
        // Keep in mind that the PA itself can keep firing as long as the HV cable under the power box has... power.
        private void OnPowerStateChanged(PowerChangedMessage e)
        {
            UpdateAppearance();

            if (!e.Powered)
            {
                UserInterface?.CloseAll();
            }
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (!ConsolePowered)
            {
                return;
            }


            if (obj.Session.AttachedEntity == null ||
                !ActionBlockerSystem.CanInteract(obj.Session.AttachedEntity))
            {
                return;
            }

            if (_wireInterfaceBlocked)
            {
                return;
            }

            switch (obj.Message)
            {
                case ParticleAcceleratorSetEnableMessage enableMessage:
                    if (enableMessage.Enabled)
                    {
                        SwitchOn();
                    }
                    else
                    {
                        SwitchOff();
                    }

                    break;

                case ParticleAcceleratorSetPowerStateMessage stateMessage:
                    SetStrength(stateMessage.State);
                    break;

                case ParticleAcceleratorRescanPartsMessage _:
                    RescanParts();
                    break;
            }

            UpdateUI();
        }

        public void UpdateUI()
        {
            var draw = 0;
            var receive = 0;

            if (_isEnabled)
            {
                draw = _partPowerBox!.PowerConsumerComponent!.DrawRate;
                receive = _partPowerBox!.PowerConsumerComponent!.ReceivedPower;
            }

            var state = new ParticleAcceleratorUIState(
                _isAssembled,
                _isEnabled,
                _selectedStrength,
                draw,
                receive,
                _partEmitterLeft != null,
                _partEmitterCenter != null,
                _partEmitterRight != null,
                _partPowerBox != null,
                _partFuelChamber != null,
                _partEndCap != null,
                _wireInterfaceBlocked,
                MaxPower,
                _wirePowerBlocked);

            UserInterface?.SetState(state);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (Owner.TryGetComponent<WiresComponent>(out var wires) && wires.IsPanelOpen)
            {
                wires.OpenInterface(actor.playerSession);
            }
            else
            {
                if (!ConsolePowered)
                {
                    return;
                }

                UserInterface?.Toggle(actor.playerSession);
                UpdateUI();
            }
        }

        public override void OnRemove()
        {
            UserInterface?.CloseAll();
            base.OnRemove();
        }

        void IWires.RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Toggle);
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Strength);
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Interface);
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Limiter);
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Nothing);
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            switch (args.Identifier)
            {
                case ParticleAcceleratorControlBoxWires.Toggle:
                    if (args.Action == WiresAction.Pulse)
                    {
                        if (_isEnabled)
                        {
                            SwitchOff();
                        }
                        else
                        {
                            SwitchOn();
                        }
                    }
                    else
                    {
                        _wirePowerBlocked = args.Action == WiresAction.Cut;
                        if (_isEnabled)
                        {
                            SwitchOff();
                        }
                    }

                    break;

                case ParticleAcceleratorControlBoxWires.Strength:
                    if (args.Action == WiresAction.Pulse)
                    {
                        SetStrength(_selectedStrength + 1);
                    }
                    else
                    {
                        _wireStrengthCut = args.Action == WiresAction.Cut;
                    }

                    break;

                case ParticleAcceleratorControlBoxWires.Interface:
                    if (args.Action == WiresAction.Pulse)
                    {
                        _wireInterfaceBlocked ^= true;
                    }
                    else
                    {
                        _wireInterfaceBlocked = args.Action == WiresAction.Cut;
                    }

                    break;

                case ParticleAcceleratorControlBoxWires.Limiter:
                    if (args.Action == WiresAction.Pulse)
                    {
                        Owner.PopupMessageEveryone(Loc.GetString("The control box makes a whirring noise."));
                    }
                    else
                    {
                        _wireLimiterCut = args.Action == WiresAction.Cut;
                        if (_selectedStrength == ParticleAcceleratorPowerState.Level3 && !_wireLimiterCut)
                        {
                            // Yes, it's a feature that mending this wire WON'T WORK if the strength wire is also cut.
                            // Since that blocks SetStrength().
                            SetStrength(ParticleAcceleratorPowerState.Level2);
                        }
                    }

                    break;
            }

            UpdateUI();
            UpdateWireStatus();
        }

        private void UpdateWireStatus()
        {
            if (!Owner.TryGetComponent(out WiresComponent? wires))
            {
                return;
            }

            var powerBlock = _wirePowerBlocked;
            var keyboardLight = new StatusLightData(Color.Green,
                _wireInterfaceBlocked
                    ? StatusLightState.BlinkingFast
                    : StatusLightState.On,
                "KEYB");

            var powerLight = new StatusLightData(
                Color.Yellow,
                powerBlock ? StatusLightState.Off : StatusLightState.On,
                "POWR");

            var limiterLight = new StatusLightData(
                _wireLimiterCut ? Color.Purple : Color.Teal,
                StatusLightState.On,
                "LIMT");

            var strengthLight = new StatusLightData(
                Color.Blue,
                _wireStrengthCut ? StatusLightState.BlinkingSlow : StatusLightState.On,
                "STRC");

            wires.SetStatus(ParticleAcceleratorWireStatus.Keyboard, keyboardLight);
            wires.SetStatus(ParticleAcceleratorWireStatus.Power, powerLight);
            wires.SetStatus(ParticleAcceleratorWireStatus.Limiter, limiterLight);
            wires.SetStatus(ParticleAcceleratorWireStatus.Strength, strengthLight);
        }

        public void RescanParts()
        {
            SwitchOff();
            foreach (var part in AllParts())
            {
                part.Master = null;
            }

            _isAssembled = false;
            _partFuelChamber = null;
            _partEndCap = null;
            _partPowerBox = null;
            _partEmitterLeft = null;
            _partEmitterCenter = null;
            _partEmitterRight = null;

            // Find fuel chamber first by scanning cardinals.
            if (Owner.Transform.Anchored)
            {
                var grid = _mapManager.GetGrid(Owner.Transform.GridID);
                var coords = Owner.Transform.Coordinates;
                foreach (var maybeFuel in grid.GetCardinalNeighborCells(coords))
                {
                    if (Owner.EntityManager.ComponentManager.TryGetComponent(maybeFuel, out _partFuelChamber))
                    {
                        break;
                    }
                }
            }

            if (_partFuelChamber == null)
            {
                UpdateUI();
                return;
            }

            // Align ourselves to match fuel chamber orientation.
            // This means that if you mess up the orientation of the control box it's not a big deal,
            // because the sprite is far from obvious about the orientation.
            Owner.Transform.LocalRotation = _partFuelChamber.Owner.Transform.LocalRotation;

            var offsetEndCap = RotateOffset((1, 1));
            var offsetPowerBox = RotateOffset((1, -1));
            var offsetEmitterLeft = RotateOffset((0, -2));
            var offsetEmitterCenter = RotateOffset((1, -2));
            var offsetEmitterRight = RotateOffset((2, -2));

            ScanPart(offsetEndCap, out _partEndCap);
            ScanPart(offsetPowerBox, out _partPowerBox);

            if (!ScanPart(offsetEmitterCenter, out _partEmitterCenter) ||
                _partEmitterCenter.Type != ParticleAcceleratorEmitterType.Center)
            {
                // if it's the wrong type we need to clear this to avoid shenanigans.
                _partEmitterCenter = null;
            }

            if (ScanPart(offsetEmitterLeft, out _partEmitterLeft) &&
                _partEmitterLeft.Type != ParticleAcceleratorEmitterType.Left)
            {
                _partEmitterLeft = null;
            }

            if (ScanPart(offsetEmitterRight, out _partEmitterRight) &&
                _partEmitterRight.Type != ParticleAcceleratorEmitterType.Right)
            {
                _partEmitterRight = null;
            }

            _isAssembled = _partFuelChamber != null &&
                           _partPowerBox != null &&
                           _partEmitterCenter != null &&
                           _partEmitterLeft != null &&
                           _partEmitterRight != null &&
                           _partEndCap != null;

            foreach (var part in AllParts())
            {
                part.Master = this;
            }

            UpdateUI();

            Vector2i RotateOffset(in Vector2i vec)
            {
                var rot = new Angle(Owner.Transform.LocalRotation);
                return (Vector2i) rot.RotateVec(vec);
            }
        }

        private bool ScanPart<T>(Vector2i offset, [NotNullWhen(true)] out T? part)
            where T : ParticleAcceleratorPartComponent
        {
            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            var coords = Owner.Transform.Coordinates;
            foreach (var ent in grid.GetOffset(coords, offset))
            {
                if (Owner.EntityManager.ComponentManager.TryGetComponent(ent, out part) && !part.Deleted)
                {
                    return true;
                }
            }

            part = default;
            return false;
        }

        private IEnumerable<ParticleAcceleratorPartComponent> AllParts()
        {
            if (_partFuelChamber != null)
                yield return _partFuelChamber;
            if (_partEndCap != null)
                yield return _partEndCap;
            if (_partPowerBox != null)
                yield return _partPowerBox;
            if (_partEmitterLeft != null)
                yield return _partEmitterLeft;
            if (_partEmitterCenter != null)
                yield return _partEmitterCenter;
            if (_partEmitterRight != null)
                yield return _partEmitterRight;
        }

        public void SwitchOn()
        {
            DebugTools.Assert(_isAssembled);

            if (_isEnabled)
            {
                return;
            }

            _isEnabled = true;
            UpdatePowerDraw();
            // If we don't have power yet we'll turn on when we receive more power from the powernet.
            // if we do we'll just go and turn on right now.
            if (_partPowerBox!.PowerConsumerComponent!.ReceivedPower >= _partPowerBox.PowerConsumerComponent.DrawRate)
            {
                PowerOn();
            }

            UpdateUI();
        }

        private void UpdatePowerDraw()
        {
            _partPowerBox!.PowerConsumerComponent!.DrawRate = PowerDrawFor(_selectedStrength);
        }

        public void SwitchOff()
        {
            _isEnabled = false;
            PowerOff();
            UpdateUI();
        }

        private void PowerOn()
        {
            DebugTools.Assert(_isEnabled);
            DebugTools.Assert(_isAssembled);

            if (_isPowered)
            {
                return;
            }

            _isPowered = true;
            UpdateFiring();
            UpdatePartVisualStates();
            UpdateUI();
        }

        private void PowerOff()
        {
            if (!_isPowered)
            {
                return;
            }

            _isPowered = false;
            UpdateFiring();
            UpdateUI();
            UpdatePartVisualStates();
        }

        public void SetStrength(ParticleAcceleratorPowerState state)
        {
            if (_wireStrengthCut)
            {
                return;
            }

            state = (ParticleAcceleratorPowerState) MathHelper.Clamp(
                (int) state,
                (int) ParticleAcceleratorPowerState.Standby,
                (int) MaxPower);

            _selectedStrength = state;
            UpdateAppearance();
            UpdatePartVisualStates();

            if (_isEnabled)
            {
                UpdatePowerDraw();
                UpdateFiring();
            }
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(ParticleAcceleratorVisuals.VisualState,
                    _powerReceiverComponent!.Powered
                        ? (ParticleAcceleratorVisualState) _selectedStrength
                        : ParticleAcceleratorVisualState.Unpowered);
            }
        }

        private void UpdateFiring()
        {
            if (!_isPowered || _selectedStrength == ParticleAcceleratorPowerState.Standby)
            {
                StopFiring();
            }
            else
            {
                StartFiring();
            }
        }

        private void StartFiring()
        {
            EverythingIsWellToFire();

            _fireCancelTokenSrc?.Cancel();
            _fireCancelTokenSrc = new CancellationTokenSource();
            var cancelToken = _fireCancelTokenSrc.Token;
            Timer.SpawnRepeating(_firingDelay, Fire, cancelToken);
        }

        private void Fire()
        {
            EverythingIsWellToFire();

            _partEmitterCenter!.Fire(_selectedStrength);
            _partEmitterLeft!.Fire(_selectedStrength);
            _partEmitterRight!.Fire(_selectedStrength);
        }

        [Conditional("DEBUG")]
        private void EverythingIsWellToFire()
        {
            DebugTools.Assert(!Deleted);
            DebugTools.Assert(_isPowered);
            DebugTools.Assert(_selectedStrength != ParticleAcceleratorPowerState.Standby);
            DebugTools.Assert(_isAssembled);
            DebugTools.Assert(_partEmitterCenter != null);
            DebugTools.Assert(_partEmitterLeft != null);
            DebugTools.Assert(_partEmitterRight != null);
        }

        private void StopFiring()
        {
            _fireCancelTokenSrc?.Cancel();
            _fireCancelTokenSrc = null;
        }

        private int PowerDrawFor(ParticleAcceleratorPowerState strength)
        {
            return strength switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 1,
                ParticleAcceleratorPowerState.Level1 => 3,
                ParticleAcceleratorPowerState.Level2 => 4,
                ParticleAcceleratorPowerState.Level3 => 5,
                _ => 0
            } * _powerDrawMult + _powerDrawBase;
        }

        public void PowerBoxReceivedChanged(object? sender, ReceivedPowerChangedEventArgs eventArgs)
        {
            DebugTools.Assert(_isAssembled);

            if (!_isEnabled)
            {
                return;
            }

            var isPowered = eventArgs.ReceivedPower >= eventArgs.DrawRate;
            if (isPowered)
            {
                PowerOn();
            }
            else
            {
                PowerOff();
            }

            UpdateUI();
        }

        private void UpdatePartVisualStates()
        {
            // UpdatePartVisualState(ControlBox);
            UpdatePartVisualState(_partFuelChamber);
            UpdatePartVisualState(_partPowerBox);
            UpdatePartVisualState(_partEmitterCenter);
            UpdatePartVisualState(_partEmitterLeft);
            UpdatePartVisualState(_partEmitterRight);
            //no endcap because it has no powerlevel-sprites
        }

        private void UpdatePartVisualState(ParticleAcceleratorPartComponent? component)
        {
            if (component == null || !component.Owner.TryGetComponent<AppearanceComponent>(out var appearanceComponent))
            {
                return;
            }

            var state = _isPowered
                ? (ParticleAcceleratorVisualState) _selectedStrength
                : ParticleAcceleratorVisualState.Unpowered;
            appearanceComponent.SetData(ParticleAcceleratorVisuals.VisualState, state);
        }

        public override void Rotated()
        {
            // We rotate OURSELVES when scanning for parts, so don't actually run rescan on rotate.
            // That would be silly.
        }

        public enum ParticleAcceleratorControlBoxWires
        {
            /// <summary>
            /// Pulse toggles Power. Cut permanently turns off until Mend.
            /// </summary>
            Toggle,

            /// <summary>
            /// Pulsing increases level until at limit.
            /// </summary>
            Strength,

            /// <summary>
            /// Pulsing toggles Button-Disabled on UI. Cut disables, Mend enables.
            /// </summary>
            Interface,

            /// <summary>
            /// Pulsing will produce short message about whirring noise. Cutting increases the max level to 3. Mending reduces it back to 2.
            /// </summary>
            Limiter,

            /// <summary>
            /// Does Nothing
            /// </summary>
            Nothing
        }
    }
}
