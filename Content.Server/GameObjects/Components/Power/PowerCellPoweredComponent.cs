#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Represents a device that has a <see cref="PowerCellSlotComponent"/> and uses the power cell in it for power.
    /// Has a power draw (wattage), an on state and an off state and can be turned on to consume its wattage in power
    /// each second.
    /// </summary>
    [RegisterComponent]
    public class PowerCellPoweredComponent : Component
    {
        public override string Name => "PowerCellPowered";

        /// <summary>
        /// How much power the device consumes when powered on.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public float Wattage = 10f;

        /// <summary>
        /// How much power the device consumes when powered off.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public float WattageStandby = 0f;

        [ViewVariables] public bool PoweredOn { get => _poweredOn; }
        private bool _poweredOn = false;

        [ViewVariables] public PowerCellSlotComponent CellSlot { get; private set; } = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref Wattage, "wattage", 10f);
            serializer.DataField(ref WattageStandby, "wattageStandby", 0f);
        }

        public override void Initialize()
        {
            base.Initialize();
            CellSlot = Owner.EnsureComponent<PowerCellSlotComponent>();
        }


        /// <summary>
        /// Try toggling the device's state.
        /// </summary>
        /// <returns>True if the state was toggled; false if it wasn't.</returns>
        public bool TryToggleState()
        {
            return _poweredOn ? TryTurnOff() : TryTurnOn();
        }

        /// <summary>
        /// Try turning the device on.
        /// </summary>
        /// <returns>True if the device is on after this operation; false if it's off.</returns>
        public bool TryTurnOn()
        {
            if (_poweredOn) return true;
            if (CellSlot?.Cell == null) return false;
            if (CellSlot.Cell.CurrentCharge < Wattage) return false;
            _poweredOn = true;
            Owner.SendMessage(this, new PowerStatusChangedMessage(true, false));
            return true;
        }

        /// <summary>
        /// Try turning the device off.
        /// </summary>
        /// <returns>True if the device is off after this operation; false if it's on.</returns>
        public bool TryTurnOff()
        {
            if (!_poweredOn) return true;
            _poweredOn = false;
            Owner.SendMessage(this, new PowerStatusChangedMessage(false, false));
            return true;
        }

        /// <summary>
        /// Turns the device off unless it's already off.
        /// </summary>
        private void TurnOff()
        {
            if (!_poweredOn) return;
            _poweredOn = false;
            Owner.SendMessage(this, new PowerStatusChangedMessage(false, false));
        }

        public void OnUpdate(float frameTime)
        {
            if (!_poweredOn) return;
            if (CellSlot?.Cell == null)
            {
                TurnOff();
                return;
            }
            var cell = CellSlot.Cell;
            if (!cell.TryUseCharge(Wattage * frameTime))
            {
                _poweredOn = false;
                Owner.SendMessage(this, new PowerStatusChangedMessage(false, true));
            };
        }
    }

    public class PowerStatusChangedMessage : ComponentMessage
    {
        /// <summary>
        /// True if the device was powered on; false if it was powered off.
        /// </summary>
        public bool PoweredOn { get; }

        /// <summary>
        /// True if the device was powered off because it ran out of battery.
        /// </summary>
        public bool RanOutOfBattery { get; }

        public PowerStatusChangedMessage(bool on, bool ranOutOfBattery)
        {
            PoweredOn = on;
            RanOutOfBattery = ranOutOfBattery;
        }
    }
}
