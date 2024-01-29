namespace Content.Server.Atmos.Components
{
    // Unfortunately can't be friends yet due to magboots.
    [RegisterComponent]
    public sealed partial class MovedByPressureComponent : Component
    {
        public const float MoveForcePushRatio = 1f;
        public const float MoveForceForcePushRatio = 1f;
        public const float ProbabilityOffset = 25f;
        public const float ProbabilityBasePercent = 10f;
        public const float ThrowForce = 100f;

        /// <summary>
        /// Accumulates time when yeeted by high pressure deltas.
        /// </summary>
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("pressureResistance")]
        public float PressureResistance { get; set; } = 1f;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("moveResist")]
        public float MoveResist { get; set; } = 100f;
        [ViewVariables(VVAccess.ReadWrite)]
        public int LastHighPressureMovementAirCycle { get; set; } = 0;
    }
}
