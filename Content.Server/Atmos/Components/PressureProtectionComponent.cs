namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class PressureProtectionComponent : Component
    {
        [DataField("highPressureMultiplier")]
        public float HighPressureMultiplier { get; } = 1f;

        [DataField("highPressureModifier")]
        public float HighPressureModifier { get; } = 0f;

        [DataField("lowPressureMultiplier")]
        public float LowPressureMultiplier { get; } = 1f;

        [DataField("lowPressureModifier")]
        public float LowPressureModifier { get; } = 0f;
    }
}
