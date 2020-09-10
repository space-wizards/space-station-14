#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;
using static Content.Shared.GameObjects.Components.SharedWiresComponent.WiresAction;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Doors
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(ServerDoorComponent))]
    public class AirlockComponent : ServerDoorComponent, IWires
    {
        public override string Name => "Airlock";

        /// <summary>
        /// Duration for which power will be disabled after pulsing either power wire.
        /// </summary>
        private static readonly TimeSpan PowerWiresTimeout = TimeSpan.FromSeconds(5.0);

        private CancellationTokenSource _powerWiresPulsedTimerCancel = new CancellationTokenSource();

        private bool _powerWiresPulsed;

        /// <summary>
        /// True if either power wire was pulsed in the last <see cref="PowerWiresTimeout"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private bool PowerWiresPulsed
        {
            get => _powerWiresPulsed;
            set
            {
                _powerWiresPulsed = value;
                UpdateWiresStatus();
                UpdatePowerCutStatus();
            }
        }

        private bool _boltsDown;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool BoltsDown
        {
            get => _boltsDown;
            set
            {
                _boltsDown = value;
                UpdateBoltLightStatus();
            }
        }

        private bool _boltLightsWirePulsed = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool BoltLightsVisible
        {
            get => _boltLightsWirePulsed && BoltsDown && IsPowered() && State == DoorState.Closed;
            set
            {
                _boltLightsWirePulsed = value;
                UpdateBoltLightStatus();
            }
        }

        private const float AutoCloseDelayFast = 1;
        // True => AutoCloseDelay; False => AutoCloseDelayFast
        [ViewVariables(VVAccess.ReadWrite)]
        private bool NormalCloseSpeed
        {
            get => CloseSpeed == AutoCloseDelay;
            set => CloseSpeed = value ? AutoCloseDelay : AutoCloseDelayFast;
        }

        private void UpdateWiresStatus()
        {
            WiresComponent? wires;
            var powerLight = new StatusLightData(Color.Yellow, StatusLightState.On, "POWR");
            if (PowerWiresPulsed)
            {
                powerLight = new StatusLightData(Color.Yellow, StatusLightState.BlinkingFast, "POWR");
            }
            else if (Owner.TryGetComponent(out wires) &&
                     wires.IsWireCut(Wires.MainPower) &&
                     wires.IsWireCut(Wires.BackupPower))
            {
                powerLight = new StatusLightData(Color.Red, StatusLightState.On, "POWR");
            }

            var boltStatus =
                new StatusLightData(Color.Red, BoltsDown ? StatusLightState.On : StatusLightState.Off, "BOLT");
            var boltLightsStatus = new StatusLightData(Color.Lime,
                _boltLightsWirePulsed ? StatusLightState.On : StatusLightState.Off, "BLTL");

            var timingStatus =
                new StatusLightData(Color.Orange,   !AutoClose ? StatusLightState.Off :
                                                    !NormalCloseSpeed ? StatusLightState.BlinkingSlow :
                                                    StatusLightState.On,
                                                    "TIME");

            var safetyStatus =
                new StatusLightData(Color.Red, Safety ? StatusLightState.On : StatusLightState.Off, "SAFE");

            if (!Owner.TryGetComponent(out wires))
            {
                return;
            }

            wires.SetStatus(AirlockWireStatus.PowerIndicator, powerLight);
            wires.SetStatus(AirlockWireStatus.BoltIndicator, boltStatus);
            wires.SetStatus(AirlockWireStatus.BoltLightIndicator, boltLightsStatus);
            wires.SetStatus(AirlockWireStatus.AIControlIndicator, new StatusLightData(Color.Purple, StatusLightState.BlinkingSlow, "AICT"));
            wires.SetStatus(AirlockWireStatus.TimingIndicator, timingStatus);
            wires.SetStatus(AirlockWireStatus.SafetyIndicator, safetyStatus);
            /*
            _wires.SetStatus(6, powerLight);
            _wires.SetStatus(7, powerLight);
            _wires.SetStatus(8, powerLight);
            _wires.SetStatus(9, powerLight);
            _wires.SetStatus(10, powerLight);
            _wires.SetStatus(11, powerLight);*/
        }

        private void UpdatePowerCutStatus()
        {
            if (!Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                return;
            }

            if (PowerWiresPulsed)
            {
                receiver.PowerDisabled = true;
                return;
            }

            if (!Owner.TryGetComponent(out WiresComponent? wires))
            {
                return;
            }

            receiver.PowerDisabled =
                wires.IsWireCut(Wires.MainPower) ||
                wires.IsWireCut(Wires.BackupPower);
        }

        private void UpdateBoltLightStatus()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DoorVisuals.BoltLights, BoltLightsVisible);
            }
        }

        public override DoorState State
        {
            protected set
            {
                base.State = value;
                // Only show the maintenance panel if the airlock is closed
                if (Owner.TryGetComponent(out WiresComponent? wires))
                {
                    wires.IsPanelVisible = value != DoorState.Open;
                }
                // If the door is closed, we should look if the bolt was locked while closing
                UpdateBoltLightStatus();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged += PowerDeviceOnOnPowerStateChanged;

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {

                    appearance.SetData(DoorVisuals.Powered, receiver.Powered);
                }
            }
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged -= PowerDeviceOnOnPowerStateChanged;
            }

            base.OnRemove();
        }

        private void PowerDeviceOnOnPowerStateChanged(object? sender, PowerStateEventArgs e)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DoorVisuals.Powered, e.Powered);
            }

            // BoltLights also got out
            UpdateBoltLightStatus();
        }

        protected override void ActivateImpl(ActivateEventArgs args)
        {
            if (Owner.TryGetComponent(out WiresComponent? wires) &&
                wires.IsPanelOpen)
            {
                if (args.User.TryGetComponent(out IActorComponent? actor))
                {
                    wires.OpenInterface(actor.playerSession);
                }
            }
            else
            {
                base.ActivateImpl(args);
            }
        }

        private enum Wires
        {
            /// <summary>
            /// Pulsing turns off power for <see cref="AirlockComponent.PowerWiresTimeout"/>.
            /// Cutting turns off power permanently if <see cref="BackupPower"/> is also cut.
            /// Mending restores power.
            /// </summary>
            MainPower,

            /// <see cref="MainPower"/>
            BackupPower,

            /// <summary>
            /// Pulsing causes for bolts to toggle (but only raise if power is on)
            /// Cutting causes Bolts to drop
            /// Mending does nothing
            /// </summary>
            Bolts,

            /// <summary>
            /// Pulsing causes light to toggle
            /// Cutting causes light to go out
            /// Mending causes them to go on again
            /// </summary>
            BoltLight,

            // Placeholder for when AI is implemented
            AIControl,

            /// <summary>
            /// Pulsing causes door to close faster
            /// Cutting disables door timer, causing door to stop closing automatically
            /// Mending restores door timer
            /// </summary>
            Timing,

            /// <summary>
            /// Pulsing toggles safety
            /// Cutting disables safety
            /// Mending enables safety
            /// </summary>
            Safety,
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.MainPower);
            builder.CreateWire(Wires.BackupPower);
            builder.CreateWire(Wires.Bolts);
            builder.CreateWire(Wires.BoltLight);
            builder.CreateWire(Wires.Timing);
            builder.CreateWire(Wires.Safety);
            /*
            builder.CreateWire(6);
            builder.CreateWire(7);
            builder.CreateWire(8);
            builder.CreateWire(9);
            builder.CreateWire(10);
            builder.CreateWire(11);*/
            UpdateWiresStatus();
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            if (args.Action == Pulse)
            {
                switch (args.Identifier)
                {
                    case Wires.MainPower:
                    case Wires.BackupPower:
                        PowerWiresPulsed = true;
                        _powerWiresPulsedTimerCancel.Cancel();
                        _powerWiresPulsedTimerCancel = new CancellationTokenSource();
                        Timer.Spawn(PowerWiresTimeout,
                            () => PowerWiresPulsed = false,
                            _powerWiresPulsedTimerCancel.Token);
                        break;
                    case Wires.Bolts:
                        if (!BoltsDown)
                        {
                            SetBoltsWithAudio(true);
                        }
                        else
                        {
                            if (IsPowered()) // only raise again if powered
                            {
                                SetBoltsWithAudio(false);
                            }
                        }

                        break;
                    case Wires.BoltLight:
                        // we need to change the property here to set the appearance again
                        BoltLightsVisible = !_boltLightsWirePulsed;
                        break;
                    case Wires.Timing:
                        NormalCloseSpeed = !NormalCloseSpeed;
                        break;
                    case Wires.Safety:
                        Safety = !Safety;
                        break;
                }
            }

            if (args.Action == Mend)
            {
                switch (args.Identifier)
                {
                    case Wires.MainPower:
                    case Wires.BackupPower:
                        // mending power wires instantly restores power
                        _powerWiresPulsedTimerCancel?.Cancel();
                        PowerWiresPulsed = false;
                        break;
                    case Wires.BoltLight:
                        BoltLightsVisible = true;
                        break;
                    case Wires.Timing:
                        AutoClose = true;
                        break;
                    case Wires.Safety:
                        Safety = true;
                        break;
                }
            }

            if (args.Action == Cut)
            {
                switch (args.Identifier)
                {
                    case Wires.Bolts:
                        SetBoltsWithAudio(true);
                        break;
                    case Wires.BoltLight:
                        BoltLightsVisible = false;
                        break;
                    case Wires.Timing:
                        AutoClose = false;
                        break;
                    case Wires.Safety:
                        Safety = false;
                        break;
                }
            }

            UpdateWiresStatus();
            UpdatePowerCutStatus();
        }

        public override bool CanOpen()
        {
            return base.CanOpen() && IsPowered() && !IsBolted();
        }

        public override bool CanClose()
        {
            return IsPowered() && !IsBolted();
        }

        public override void Deny()
        {
            if (!IsPowered() || IsBolted())
            {
                return;
            }

            base.Deny();
        }

        private bool IsBolted()
        {
            return _boltsDown;
        }

        private bool IsPowered()
        {
            return !Owner.TryGetComponent(out PowerReceiverComponent? receiver)
                   || receiver.Powered;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (await base.InteractUsing(eventArgs))
                return true;

            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Cutting)
                || tool.HasQuality(ToolQuality.Multitool))
            {
                if (Owner.TryGetComponent(out WiresComponent? wires)
                    && wires.IsPanelOpen)
                {
                    if (eventArgs.User.TryGetComponent(out IActorComponent? actor))
                    {
                        wires.OpenInterface(actor.playerSession);
                        return true;
                    }
                }
            }

            bool AirlockCheck()
            {
                if (IsBolted())
                {
                    Owner.PopupMessage(eventArgs.User,
                        Loc.GetString("The airlock's bolts prevent it from being forced!"));
                    return false;
                }

                if (IsPowered())
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("The powered motors block your efforts!"));
                    return false;
                }

                return true;
            }

            if (!await tool.UseTool(eventArgs.User, Owner, 0.2f, ToolQuality.Prying, AirlockCheck)) return false;

            if (State == DoorState.Closed)
                Open();
            else if (State == DoorState.Open)
                Close();

            return true;
        }

        public void SetBoltsWithAudio(bool newBolts)
        {
            if (newBolts == BoltsDown)
            {
                return;
            }

            BoltsDown = newBolts;

            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity(newBolts ? "/Audio/Machines/boltsdown.ogg" : "/Audio/Machines/boltsup.ogg", Owner);
        }
    }
}
