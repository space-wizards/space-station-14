using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityFlags.Prototypes;

[Prototype("entityFlag")]
public sealed class EntityFlagPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;
    [DataField("local")] public bool Local;

    [DataField("flagGroup", customTypeSerializer: typeof(PrototypeIdSerializer<EntityFlagGroupPrototype>))]
    public string? FlagGroup;
}
