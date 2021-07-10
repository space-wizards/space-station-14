#nullable enable
using System;
using System.Threading;
using Content.Server.Power.Components;
using Content.Server.VendingMachines;
using Content.Server.WireHacking;
using Content.Shared.Doors;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.Wires.SharedWiresComponent;
using static Content.Shared.Wires.SharedWiresComponent.WiresAction;

namespace Content.Server.Doors.Components
{
    /// <summary>
    /// Companion component to ServerDoorComponent that handles airlock-specific behavior -- wires, requiring power to operate, bolts, and allowing automatic closing.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDoorCheck))]
    public class AirlockComponent : Component, IWires, IDoorCheck
    {
        public override string Name => "Airlock";

        [ComponentDependency]
        private readonly ServerDoorComponent? _doorComponent = null;

        [ComponentDependency]
        private readonly SharedAppearanceComponent? _appearanceComponent = null;

        [ComponentDependency]
        private readonly ApcPowerReceiverComponent? _receiverComponent = null;

        [ComponentDependency]
        private readonly WiresComponent? _wiresComponent = null;

        /// <summary>
        /// Duration for which power will be disabled after pulsing either power wire.
        /// </summary>
        private static readonly TimeSpan PowerWiresTimeout = TimeSpan.FromSeconds(5.0);

        private CancellationTokenSource _powerWiresPulsedTimerCancel = new();

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
        public bool BoltsDown
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
            get => _boltLightsWirePulsed && BoltsDown && IsPowered()
                && _doorComponent != null && _doorComponent.State == SharedDoorComponent.DoorState.Closed;
            set
            {
                _boltLightsWirePulsed = value;
                UpdateBoltLightStatus();
            }
        }

        [DataField("setBoltsDownSound")] private SoundSpecifier _setBoltsDownSound = new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg");

        [DataField("setBoltsUpSound")] private SoundSpecifier _setBoltsUpSound = new SoundPathSpecifier("/Audio/Machines/boltsup.ogg");

        private static readonly TimeSpan AutoCloseDelayFast = TimeSpan.FromSeconds(1);

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _autoClose = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _normalCloseSpeed = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _safety = true;

        protected override void Initialize()
        {
            base.Initialize();

            if (_receiverComponent != null && _appearanceComponent != null)
            {
                _appearanceComponent.SetData(DoorVisuals.Powered, _receiverComponent.Powered);
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerDeviceOnOnPowerStateChanged(powerChanged);
                    break;
            }
        }

        void IDoorCheck.OnStateChange(SharedDoorComponent.DoorState doorState)
        {
            // Only show the maintenance panel if the airlock is closed
            if (_wiresComponent != null)
            {
                _wiresComponent.IsPanelVisible = doorState != SharedDoorComponent.DoorState.Open;
            }
            // If the door is closed, we should look if the bolt was locked while closing
            UpdateBoltLightStatus();
        }

        bool IDoorCheck.OpenCheck() => CanChangeState();

        bool IDoorCheck.CloseCheck() => CanChangeState();

        bool IDoorCheck.DenyCheck() => CanChangeState();

        bool IDoorCheck.SafetyCheck() => _safety;

        bool IDoorCheck.AutoCloseCheck() => _autoClose;

        TimeSpan? IDoorCheck.GetCloseSpeed()
        {
            if (_normalCloseSpeed)
            {
                return null;
            }
            return AutoCloseDelayFast;
        }

        bool IDoorCheck.BlockActivate(ActivateEventArgs eventArgs)
        {
            if (_wiresComponent != null && _wiresComponent.IsPanelOpen &&
                eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                _wiresComponent.OpenInterface(actor.PlayerSession);
                return true;
            }
            return false;
        }

        bool IDoorCheck.CanPryCheck(InteractUsingEventArgs eventArgs)
        {
            if (IsBolted())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("airlock-component-cannot-pry-is-bolted-message "));
                return false;
            }
            if (IsPowered())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("airlock-component-cannot-pry-is-powered-message"));
                return false;
            }
            return true;
        }

        private bool CanChangeState()
        {
            return IsPowered() && !IsBolted();
        }

        private bool IsBolted()
        {
            return _boltsDown;
        }

        private bool IsPowered()
        {
            return _receiverComponent == null || _receiverComponent.Powered;
        }

        private void UpdateBoltLightStatus()
        {
            if (_appearanceComponent != null)
            {
                _appearanceComponent.SetData(DoorVisuals.BoltLights, BoltLightsVisible);
            }
        }

        private void UpdateWiresStatus()
        {
            if (_doorComponent == null)
            {
                return;
            }

            var powerLight = new StatusLightData(Color.Yellow, StatusLightState.On, "POWR");
            if (PowerWiresPulsed)
            {
                powerLight = new StatusLightData(Color.Yellow, StatusLightState.BlinkingFast, "POWR");
            }
            else if (_wiresComponent != null &&
                     _wiresComponent.IsWireCut(Wires.MainPower) &&
                     _wiresComponent.IsWireCut(Wires.BackupPower))
            {
                powerLight = new StatusLightData(Color.Red, StatusLightState.On, "POWR");
            }

            var boltStatus =
                new StatusLightData(Color.Red, BoltsDown ? StatusLightState.On : StatusLightState.Off, "BOLT");
            var boltLightsStatus = new StatusLightData(Color.Lime,
                _boltLightsWirePulsed ? StatusLightState.On : StatusLightState.Off, "BLTL");

            var timingStatus =
                new StatusLightData(Color.Orange, !_autoClose ? StatusLightState.Off :
                                                    !_normalCloseSpeed ? StatusLightState.BlinkingSlow :
                                                    StatusLightState.On,
                                                    "TIME");

            var safetyStatus =
                new StatusLightData(Color.Red, _safety ? StatusLightState.On : StatusLightState.Off, "SAFE");

            if (_wiresComponent == null)
            {
                return;
            }

            _wiresComponent.SetStatus(AirlockWireStatus.PowerIndicator, powerLight);
            _wiresComponent.SetStatus(AirlockWireStatus.BoltIndicator, boltStatus);
            _wiresComponent.SetStatus(AirlockWireStatus.BoltLightIndicator, boltLightsStatus);
            _wiresComponent.SetStatus(AirlockWireStatus.AIControlIndicator, new StatusLightData(Color.Purple, StatusLightState.BlinkingSlow, "AICT"));
            _wiresComponent.SetStatus(AirlockWireStatus.TimingIndicator, timingStatus);
            _wiresComponent.SetStatus(AirlockWireStatus.SafetyIndicator, safetyStatus);
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
            if (_receiverComponent == null)
            {
                return;
            }

            if (PowerWiresPulsed)
            {
                _receiverComponent.PowerDisabled = true;
                return;
            }

            if (_wiresComponent == null)
            {
                return;
            }

            _receiverComponent.PowerDisabled =
                _wiresComponent.IsWireCut(Wires.MainPower) ||
                _wiresComponent.IsWireCut(Wires.BackupPower);
        }

        private void PowerDeviceOnOnPowerStateChanged(PowerChangedMessage e)
        {
            if (_appearanceComponent != null)
            {
                _appearanceComponent.SetData(DoorVisuals.Powered, e.Powered);
            }

            // BoltLights also got out
            UpdateBoltLightStatus();
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
            if(_doorComponent == null)
            {
                return;
            }

            if (args.Action == Pulse)
            {
                switch (args.Identifier)
                {
                    case Wires.MainPower:
                    case Wires.BackupPower:
                        PowerWiresPulsed = true;
                        _powerWiresPulsedTimerCancel.Cancel();
                        _powerWiresPulsedTimerCancel = new CancellationTokenSource();
                        Owner.SpawnTimer(PowerWiresTimeout,
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
                        _normalCloseSpeed = !_normalCloseSpeed;
                        _doorComponent.RefreshAutoClose();
                        break;
                    case Wires.Safety:
                        _safety = !_safety;
                        break;
                }
            }

            else if (args.Action == Mend)
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
                        _autoClose = true;
                        _doorComponent.RefreshAutoClose();
                        break;
                    case Wires.Safety:
                        _safety = true;
                        break;
                }
            }

            else if (args.Action == Cut)
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
                        _autoClose = false;
                        _doorComponent.RefreshAutoClose();
                        break;
                    case Wires.Safety:
                        _safety = false;
                        break;
                }
            }

            UpdateWiresStatus();
            UpdatePowerCutStatus();
        }

        public void SetBoltsWithAudio(bool newBolts)
        {
            if (newBolts == BoltsDown)
            {
                return;
            }

            BoltsDown = newBolts;

            if (newBolts)
            {
                if (_setBoltsDownSound.TryGetSound(out var boltsDownSound))
                    SoundSystem.Play(Filter.Broadcast(), boltsDownSound, Owner);
            }
            else
            {
                if (_setBoltsUpSound.TryGetSound(out var boltsUpSound))
                    SoundSystem.Play(Filter.Broadcast(), boltsUpSound, Owner);
            }
        }
    }
}
