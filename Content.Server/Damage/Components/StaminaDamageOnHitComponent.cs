using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed class StaminaDamageOnHitComponent : Component, IExamineGroup
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 30f;

    [DataField("examineGroup", customTypeSerializer: typeof(PrototypeIdSerializer<ExamineGroupPrototype>))]
    public string ExamineGroup { get; set; } = "melee";

    [DataField("examinePriority")]
    public float ExaminePriority { get; set; } = 0;
}
