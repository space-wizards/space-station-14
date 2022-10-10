using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Armor
{
    [RegisterComponent]
    public sealed class ArmorComponent : Component, IExamineGroup
    {
        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;

        [DataField("examineGroup", customTypeSerializer:typeof(PrototypeIdSerializer<ExamineGroupPrototype>))] 
        public string ExamineGroup { get; set; } = "worn-stats";

        [DataField("examinePriority")]
        public float ExaminePriority { get; set; } = 2.0f;
    }
}
