namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed partial class PressureProtectionComponent : Component
    {
        [DataField("highPressureMultiplier")]
        public float HighPressureMultiplier { get; private set; } = 1f;

        [DataField("highPressureModifier")]
        public float HighPressureModifier { get; private set; } = 0f;

        [DataField("lowPressureMultiplier")]
        public float LowPressureMultiplier { get; private set; } = 1f;

        [DataField("lowPressureModifier")]
        public float LowPressureModifier { get; private set; } = 0f;
    }
}
