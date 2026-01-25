using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Mining;

/// <summary>
/// This is a prototype for defining ores that generate in rock
/// </summary>
[Prototype]
public sealed partial class OrePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public EntProtoId? OreEntity;

    [DataField]
    public int MinOreYield = 1;

    [DataField]
    public int MaxOreYield = 1;

    [DataField]
    public SpriteSpecifier? OreSprite;
}
