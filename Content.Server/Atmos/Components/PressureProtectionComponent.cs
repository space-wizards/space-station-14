using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class PressureProtectionComponent : Component, IExamineGroup
    {
        [DataField("highPressureMultiplier")]
        public float HighPressureMultiplier { get; } = 1f;

        [DataField("highPressureModifier")]
        public float HighPressureModifier { get; } = 0f;

        [DataField("lowPressureMultiplier")]
        public float LowPressureMultiplier { get; } = 1f;

        [DataField("lowPressureModifier")]
        public float LowPressureModifier { get; } = 0f;

        [DataField("examineGroup", customTypeSerializer: typeof(PrototypeIdSerializer<ExamineGroupPrototype>))]
        public string ExamineGroup { get; set; } = "atmos";

        [DataField("examinePriority")]
        public float ExaminePriority { get; set; } = 2.0f;
    }
}
