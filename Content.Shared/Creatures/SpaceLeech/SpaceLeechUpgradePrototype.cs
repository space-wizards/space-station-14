using Robust.Shared.Prototypes;

namespace Content.Shared.Creatures.SpaceLeech;

[Prototype("SpaceLeechUpgrade")]
public sealed partial class SpaceLeechUpgradePrototype : IPrototype
{
    public const int MaxRank = 3;

    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string Stat = string.Empty;

    /// <summary>Blood cost for rank 1, 2, 3 (index 0 = rank 1).</summary>
    [DataField(required: true)]
    public int[] Costs = Array.Empty<int>();

    /// <summary>UI description shown per rank (index 0 = rank 1).</summary>
    [DataField(required: true)]
    public string[] Effects = Array.Empty<string>();

    /// <summary>
    /// Numeric magnitudes indexed by rank (0 = base/unpurchased, 1-3 = purchased).
    /// Interpretation is upgrade-specific - see SpaceLeechSystem for usage.
    /// </summary>
    [DataField]
    public float[] Magnitudes = { 0f, 0f, 0f, 0f };
}
