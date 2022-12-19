using Robust.Shared.Prototypes;

namespace Content.Shared.EntityFlags.Prototypes;

[Prototype("entityFlagGroup")]
public sealed class EntityFlagGroupPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;
    [DataField("GroupId", required: true)] public byte GroupId;
}
