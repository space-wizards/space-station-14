using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Mining;

/// <summary>
/// This is a prototype for defining ores that generate in rock
/// </summary>
[Prototype("ore")]
public sealed class OrePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("oreEntity", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? OreEntity;

    [DataField("minOreYield")]
    public int MinOreYield = 1;

    [DataField("maxOreYield")]
    public int MaxOreYield = 1;

    //TODO: add sprites for ores for things like mining analyzer
}
