using System;
using System.Threading;
using Content.Server.Power.Components;
using Content.Server.VendingMachines;
using Content.Server.WireHacking;
using Content.Shared.Doors;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
    public class AirlockComponent : Component, IWires
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Name => "Airlock";

        /// <summary>
        /// Sound to play when the bolts on the airlock go up.
        /// </summary>
        [DataField("boltUpSound")]
        public SoundSpecifier BoltUpSound = new SoundPathSpecifier("/Audio/Machines/boltsup.ogg");

        /// <summary>
        /// Sound to play when the bolts on the airlock go down.
        /// </summary>
        [DataField("boltDownSound")]
        public SoundSpecifier BoltDownSound = new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg");

        /// <summary>
        /// Duration for which power will be disabled after pulsing either power wire.
        /// </summary>
        [DataField("powerWiresTimeout")]
        public float PowerWiresTimeout = 5.0f;

        /// <summary>
        /// Whether the maintenance panel should be visible even if the airlock is opened.
        /// </summary>
        [DataField("openPanelVisible")]
        public bool OpenPanelVisible = false;

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
                && _entityManager.TryGetComponent<ServerDoorComponent>(Owner, out var doorComponent) && doorComponent.State == SharedDoorComponent.DoorState.Closed;
            set
            {
                _boltLightsWirePulsed = value;
                UpdateBoltLightStatus();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("autoClose")]
        public bool AutoClose = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("autoCloseDelayModifier")]
        public float AutoCloseDelayModifier = 1.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("safety")]
        public bool Safety = true;

        protected override void Initialize()
        {
            base.Initialize();

            if (_entityManager.TryGetComponent<ApcPowerReceiverComponent>(Owner, out var receiverComponent) &&
                _entityManager.TryGetComponent<AppearanceComponent>(Owner, out var appearanceComponent))
            {
                appearanceComponent.SetData(DoorVisuals.Powered, receiverComponent.Powered);
            }
        }

        public bool CanChangeState()
        {
            return IsPowered() && !IsBolted();
        }

        public bool IsBolted()
        {
            return _boltsDown;
        }

        public bool IsPowered()
        {
            return !_entityManager.TryGetComponent<ApcPowerReceiverComponent>(Owner, out var receiverComponent) || receiverComponent.Powered;
        }

        public void UpdateBoltLightStatus()
        {
            if (_entityManager.TryGetComponent<AppearanceComponent>(Owner, out var appearanceComponent))
            {
                appearanceComponent.SetData(DoorVisuals.BoltLights, BoltLightsVisible);
            }
        }

        public void UpdateWiresStatus()
        {
            if (!_entityManager.TryGetComponent<WiresComponent>(Owner, out var wiresComponent)) return;

            var mainPowerCut = wiresComponent.IsWireCut(Wires.MainPower);
            var backupPowerCut = wiresComponent.IsWireCut(Wires.BackupPower);
            var statusLightState = PowerWiresPulsed ? StatusLightState.BlinkingFast : StatusLightState.On;
            StatusLightData powerLight;
            if (mainPowerCut && backupPowerCut)
            {
                powerLight = new StatusLightData(Color.DarkGoldenrod, StatusLightState.Off, "POWER");
            }
            else if (mainPowerCut != backupPowerCut)
            {
                powerLight = new StatusLightData(Color.Gold, statusLightState, "POWER");
            }
            else
            {
                powerLight = new StatusLightData(Color.Yellow, statusLightState, "POWER");
            }

            var boltStatus =
                new StatusLightData(Color.Red, BoltsDown ? StatusLightState.On : StatusLightState.Off, "BOLT");
            var boltLightsStatus = new StatusLightData(Color.Lime,
                _boltLightsWirePulsed ? StatusLightState.On : StatusLightState.Off, "BOLT LED");

            var ev = new DoorGetCloseTimeModifierEvent();
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, ev, false);

            var timingStatus =
                new StatusLightData(Color.Orange, !AutoClose ? StatusLightState.Off :
                                                    !MathHelper.CloseToPercent(ev.CloseTimeModifier, 1.0f) ? StatusLightState.BlinkingSlow :
                                                    StatusLightState.On,
                                                    "TIME");

            var safetyStatus =
                new StatusLightData(Color.Red, Safety ? StatusLightState.On : StatusLightState.Off, "SAFETY");


            wiresComponent.SetStatus(AirlockWireStatus.PowerIndicator, powerLight);
            wiresComponent.SetStatus(AirlockWireStatus.BoltIndicator, boltStatus);
            wiresComponent.SetStatus(AirlockWireStatus.BoltLightIndicator, boltLightsStatus);
            wiresComponent.SetStatus(AirlockWireStatus.AIControlIndicator, new StatusLightData(Color.Purple, StatusLightState.BlinkingSlow, "AI CTRL"));
            wiresComponent.SetStatus(AirlockWireStatus.TimingIndicator, timingStatus);
            wiresComponent.SetStatus(AirlockWireStatus.SafetyIndicator, safetyStatus);
        }

        private void UpdatePowerCutStatus()
        {
            if (!_entityManager.TryGetComponent<ApcPowerReceiverComponent>(Owner, out var receiverComponent))
            {
                return;
            }

            if (PowerWiresPulsed)
            {
                receiverComponent.PowerDisabled = true;
                return;
            }

            if (!_entityManager.TryGetComponent<WiresComponent>(Owner, out var wiresComponent))
            {
                return;
            }

            receiverComponent.PowerDisabled =
                wiresComponent.IsWireCut(Wires.MainPower) &&
                wiresComponent.IsWireCut(Wires.BackupPower);
        }

        private void PowerDeviceOnOnPowerStateChanged(PowerChangedMessage e)
        {
            if (_entityManager.TryGetComponent<AppearanceComponent>(Owner, out var appearanceComponent))
            {
                appearanceComponent.SetData(DoorVisuals.Powered, e.Powered);
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

            UpdateWiresStatus();
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            if (!_entityManager.TryGetComponent<ServerDoorComponent>(Owner, out var doorComponent))
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
                        Owner.SpawnTimer(TimeSpan.FromSeconds(PowerWiresTimeout),
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
                        AutoCloseDelayModifier = 0.5f;
                        doorComponent.RefreshAutoClose();
                        break;
                    case Wires.Safety:
                        Safety = !Safety;
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
                        AutoClose = true;
                        doorComponent.RefreshAutoClose();
                        break;
                    case Wires.Safety:
                        Safety = true;
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
                        AutoClose = false;
                        doorComponent.RefreshAutoClose();
                        break;
                    case Wires.Safety:
                        Safety = false;
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

            SoundSystem.Play(Filter.Broadcast(), newBolts ? BoltDownSound.GetSound() : BoltUpSound.GetSound(), Owner);
        }
    }
}
