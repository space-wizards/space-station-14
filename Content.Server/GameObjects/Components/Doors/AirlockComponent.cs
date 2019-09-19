using System;
using System.Threading;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Content.Shared.GameObjects.Components.SharedWiresComponent.WiresAction;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Doors
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(ServerDoorComponent))]
    public class AirlockComponent : ServerDoorComponent, IWires, IAttackBy
    {
        public override string Name => "Airlock";

#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationMgr;
#pragma warning restore 649

        /// <summary>
        /// Duration for which power will be disabled after pulsing either power wire.
        /// </summary>
        private static readonly TimeSpan PowerWiresTimeout = TimeSpan.FromSeconds(5.0);

        private PowerDeviceComponent _powerDevice;
        private WiresComponent _wires;

        private CancellationTokenSource _powerWiresPulsedTimerCancel;

        private bool _powerWiresPulsed;
        /// <summary>
        /// True if either power wire was pulsed in the last <see cref="PowerWiresTimeout"/>.
        /// </summary>
        private bool PowerWiresPulsed
        {
            get => _powerWiresPulsed;
            set
            {
                _powerWiresPulsed = value;
                UpdateWiresStatus();
            }
        }

        private void UpdateWiresStatus()
        {
            var powerMessage = "A yellow light is on.";
            if (_powerWiresPulsed)
            {
                powerMessage = "A yellow light is blinking rapidly.";
            } else if (_wires.IsWireCut(Wires.MainPower) &&
                       _wires.IsWireCut(Wires.BackupPower))
            {
                powerMessage = "A red light is on.";
            }
            _wires.SetStatus(WiresStatus.PowerIndicator, _localizationMgr.GetString(powerMessage));
        }

        protected override DoorState State
        {
            set
            {
                base.State = value;
                // Only show the maintenance panel if the airlock is closed
                _wires.IsPanelVisible = value == DoorState.Closed;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _wires = Owner.GetComponent<WiresComponent>();
        }

        protected override void ActivateImpl(ActivateEventArgs args)
        {
            if (_wires.IsPanelOpen)
            {
                if (args.User.TryGetComponent(out IActorComponent actor))
                {
                    _wires.OpenInterface(actor.playerSession);
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
        }

        private enum WiresStatus
        {
            PowerIndicator,
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.MainPower);
            builder.CreateWire(Wires.BackupPower);
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
                        _powerWiresPulsedTimerCancel?.Cancel();
                        _powerWiresPulsedTimerCancel = new CancellationTokenSource();
                        Timer.Spawn(PowerWiresTimeout,
                            () => PowerWiresPulsed = false,
                            _powerWiresPulsedTimerCancel.Token);
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
                }
            }

            UpdateWiresStatus();
        }

        public override bool CanOpen()
        {
            return IsPowered();
        }

        public override void Deny()
        {
            if (!IsPowered())
            {
                return;
            }
            base.Deny();
        }

        private bool IsPowered()
        {
            if (PowerWiresPulsed)
            {
                return false;
            }
            if (_wires.IsWireCut(Wires.MainPower) &&
                _wires.IsWireCut(Wires.BackupPower))
            {
                return false;
            }
            return _powerDevice.Powered;
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (eventArgs.AttackWith.HasComponent<CrowbarComponent>() && !IsPowered())
            {
                if (State == DoorState.Closed)
                {
                    Open();
                }
                else if(State == DoorState.Open)
                {
                    Close();
                }
                return true;
            }

            return false;
        }
    }
}
