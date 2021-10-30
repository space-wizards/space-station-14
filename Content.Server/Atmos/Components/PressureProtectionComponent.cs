using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public class PressureProtectionComponent : Component
    {
        public override string Name => "PressureProtection";

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
