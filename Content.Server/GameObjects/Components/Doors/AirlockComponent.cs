using System;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.GameObjects.Components.SharedWiresComponent.WiresAction;

namespace Content.Server.GameObjects.Components.Doors
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class AirlockComponent : ServerDoorComponent, IWires, IAttackBy
    {
        public override string Name => "Airlock";

        /// <summary>
        /// Duration for which power will be disabled after pulsing either power wire.
        /// </summary>
        private readonly TimeSpan _powerWireTimeout = TimeSpan.FromSeconds(5.0);

        private PowerDeviceComponent _powerDevice;
        private WiresComponent _wires;

        /// <summary>
        /// Last time either power wire was pulsed.
        /// </summary>
        private DateTime _lastPowerPulse = DateTime.MinValue;

        public override void Initialize()
        {
            base.Initialize();
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _wires = Owner.GetComponent<WiresComponent>();
        }

        protected override void ActivateImpl(ActivateEventArgs args)
        {
            if (_wires.IsOpen)
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
            /// Pulsing turns off power for <see cref="AirlockComponent._powerWireTimeout"/>.
            /// Cutting turns off power permanently if <see cref="BackupPower"/> is also cut.
            /// Mending restores power.
            /// </summary>
            MainPower,
            /// <see cref="MainPower"/>
            BackupPower,
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.MainPower);
            builder.CreateWire(Wires.BackupPower);
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            if (args.Action == Pulse)
            {
                switch (args.Identifier)
                {
                    case Wires.MainPower:
                    case Wires.BackupPower:
                        _lastPowerPulse = DateTime.Now;
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
                        _lastPowerPulse = DateTime.MinValue;
                        break;
                }
            }
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
            var now = DateTime.Now;
            if (now.Subtract(_lastPowerPulse) < _powerWireTimeout)
            {
                return false;
            }
            if (_wires.IsWireCut(Wires.MainPower))
            {
                if (_wires.IsWireCut(Wires.BackupPower))
                {
                    return false;
                }
            }
            return _powerDevice.Powered;
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (eventArgs.AttackWith.HasComponent<CrowbarComponent>() && !IsPowered())
            {
                if (_state == DoorState.Closed)
                {
                    Open();
                }
                else if(_state == DoorState.Open)
                {
                    Close();
                }
                return true;
            }

            return false;
        }
    }
}
