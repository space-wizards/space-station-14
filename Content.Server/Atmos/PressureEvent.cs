using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos
{
    public abstract class PressureEvent : EntityEventArgs, IInventoryRelayEvent
    {
        /// <summary>
        ///     The environment pressure.
        /// </summary>
        public float Pressure { get; }

        /// <summary>
        ///     The modifier for the apparent pressure.
        ///     This number will be added to the environment pressure for calculation purposes.
        ///     It can be negative to reduce the felt pressure, or positive to increase it.
        /// </summary>
        /// <remarks>
        ///     Do not set this directly. Add to it, or subtract from it to modify it.
        /// </remarks>
        public float Modifier { get; set; } = 0f;

        /// <summary>
        ///     The multiplier for the apparent pressure.
        ///     The environment pressure will be multiplied by this for calculation purposes.
        /// </summary>
        /// <remarks>
        ///     Do not set, add to or subtract from this directly. Multiply this by your multiplier only.
        /// </remarks>
        public float Multiplier { get; set; } = 1f;

        /// <summary>
        ///     The inventory slots that should be checked for pressure protecting equipment.
        /// </summary>
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

        protected PressureEvent(float pressure)
        {
            Pressure = pressure;
        }
    }

    public class LowPressureEvent : PressureEvent
    {
        public LowPressureEvent(float pressure) : base(pressure) { }
    }

    public class HighPressureEvent : PressureEvent
    {
        public HighPressureEvent(float pressure) : base(pressure) { }
    }
}
