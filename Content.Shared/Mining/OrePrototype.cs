using Robust.Shared.Prototypes;

namespace Content.Shared.Mining;

/// <summary>
/// This is a prototype for defining ores that generate in rock
/// </summary>
[Prototype("Ore")]
public sealed class OrePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("oreEntity", customTypeSerializer: typeof(EntityPrototype))]
    public string? OreEntity;

    [DataField("minOreYield")]
    public int MinOreYield = 1;

    [DataField("maxOreYield")]
    public int MaxOreYield = 1;
}
