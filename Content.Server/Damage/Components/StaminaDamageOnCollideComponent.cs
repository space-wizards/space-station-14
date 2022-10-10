using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Damage.Components;

/// <summary>
/// Applies stamina damage when colliding with an entity.
/// </summary>
[RegisterComponent]
public sealed class StaminaDamageOnCollideComponent : Component, IExamineGroup
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 55f;

    [DataField("examineGroup", customTypeSerializer: typeof(PrototypeIdSerializer<ExamineGroupPrototype>))]
    public string ExamineGroup { get; set; } = "gun";

    [DataField("examinePriority")]
    public float ExaminePriority { get; set; } = 0;
}
