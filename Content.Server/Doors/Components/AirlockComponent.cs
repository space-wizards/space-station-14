using System;
using System.Threading;
using Content.Server.Power.Components;
using Content.Server.VendingMachines;
using Content.Server.WireHacking;
using Content.Shared.Doors;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
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
        public override string Name => "Airlock";

        [ComponentDependency]
        public readonly ServerDoorComponent? DoorComponent = null;

        [ComponentDependency]
        public readonly SharedAppearanceComponent? AppearanceComponent = null;

        [ComponentDependency]
        public readonly ApcPowerReceiverComponent? ReceiverComponent = null;

        [ComponentDependency]
        public readonly WiresComponent? WiresComponent = null;

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
                && DoorComponent != null && DoorComponent.State == SharedDoorComponent.DoorState.Closed;
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

            if (ReceiverComponent != null && AppearanceComponent != null)
            {
                AppearanceComponent.SetData(DoorVisuals.Powered, ReceiverComponent.Powered);
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
            return ReceiverComponent == null || ReceiverComponent.Powered;
        }

        public void UpdateBoltLightStatus()
        {
            if (AppearanceComponent != null)
            {
                AppearanceComponent.SetData(DoorVisuals.BoltLights, BoltLightsVisible);
            }
        }

        public void UpdateWiresStatus()
        {
            if (DoorComponent == null)
            {
                return;
            }

            var powerLight = new StatusLightData(Color.Yellow, StatusLightState.On, "POWR");
            if (PowerWiresPulsed)
            {
                powerLight = new StatusLightData(Color.Yellow, StatusLightState.BlinkingFast, "POWR");
            }
            else if (WiresComponent != null &&
                     WiresComponent.IsWireCut(Wires.MainPower) &&
                     WiresComponent.IsWireCut(Wires.BackupPower))
            {
                powerLight = new StatusLightData(Color.Red, StatusLightState.On, "POWR");
            }

            var boltStatus =
                new StatusLightData(Color.Red, BoltsDown ? StatusLightState.On : StatusLightState.Off, "BOLT");
            var boltLightsStatus = new StatusLightData(Color.Lime,
                _boltLightsWirePulsed ? StatusLightState.On : StatusLightState.Off, "BLTL");

            var ev = new DoorGetCloseTimeModifierEvent();
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, ev, false);

            var timingStatus =
                new StatusLightData(Color.Orange, !AutoClose ? StatusLightState.Off :
                                                    !MathHelper.CloseToPercent(ev.CloseTimeModifier, 1.0f) ? StatusLightState.BlinkingSlow :
                                                    StatusLightState.On,
                                                    "TIME");

            var safetyStatus =
                new StatusLightData(Color.Red, Safety ? StatusLightState.On : StatusLightState.Off, "SAFE");

            if (WiresComponent == null)
            {
                return;
            }

            WiresComponent.SetStatus(AirlockWireStatus.PowerIndicator, powerLight);
            WiresComponent.SetStatus(AirlockWireStatus.BoltIndicator, boltStatus);
            WiresComponent.SetStatus(AirlockWireStatus.BoltLightIndicator, boltLightsStatus);
            WiresComponent.SetStatus(AirlockWireStatus.AIControlIndicator, new StatusLightData(Color.Purple, StatusLightState.BlinkingSlow, "AICT"));
            WiresComponent.SetStatus(AirlockWireStatus.TimingIndicator, timingStatus);
            WiresComponent.SetStatus(AirlockWireStatus.SafetyIndicator, safetyStatus);
        }

        private void UpdatePowerCutStatus()
        {
            if (ReceiverComponent == null)
            {
                return;
            }

            if (PowerWiresPulsed)
            {
                ReceiverComponent.PowerDisabled = true;
                return;
            }

            if (WiresComponent == null)
            {
                return;
            }

            ReceiverComponent.PowerDisabled =
                WiresComponent.IsWireCut(Wires.MainPower) ||
                WiresComponent.IsWireCut(Wires.BackupPower);
        }

        private void PowerDeviceOnOnPowerStateChanged(PowerChangedMessage e)
        {
            if (AppearanceComponent != null)
            {
                AppearanceComponent.SetData(DoorVisuals.Powered, e.Powered);
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
            if (DoorComponent == null)
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
                        DoorComponent.RefreshAutoClose();
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
                        DoorComponent.RefreshAutoClose();
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
                        DoorComponent.RefreshAutoClose();
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
