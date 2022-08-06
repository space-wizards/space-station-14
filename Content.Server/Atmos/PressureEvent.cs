namespace Content.Server.Atmos
{
    public abstract class PressureEvent : EntityEventArgs
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

        protected PressureEvent(float pressure)
        {
            Pressure = pressure;
        }
    }

    public sealed class LowPressureEvent : PressureEvent
    {
        public LowPressureEvent(float pressure) : base(pressure) { }
    }

    public sealed class HighPressureEvent : PressureEvent
    {
        public HighPressureEvent(float pressure) : base(pressure) { }
    }
}
