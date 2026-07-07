using Robust.Shared.Prototypes;

namespace Content.Shared.Creatures.SpaceLeech;

/// <summary>
/// A single purchasable upgrade in the space leech's evolution menu, with a blood cost
/// and effect magnitude per rank.
/// </summary>
[Prototype]
public sealed partial class SpaceLeechUpgradePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>Sort order in the upgrade menu, ascending. Ties are broken by ID.</summary>
    [DataField]
    public int Order;

    /// <summary>Display name of the upgrade.</summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>Short category tag shown on the upgrade row (e.g. ATTACK, MOVE).</summary>
    [DataField(required: true)]
    public LocId Stat;

    /// <summary>Blood cost per rank (index 0 = rank 1). The list length is the max rank.</summary>
    [DataField(required: true)]
    public List<int> Costs = new();

    /// <summary>UI description shown per rank (index 0 = rank 1). Must match <see cref="Costs"/> in length.</summary>
    [DataField(required: true)]
    public List<LocId> Effects = new();

    /// <summary>
    /// Numeric magnitudes indexed by rank (0 = base/unpurchased, 1+ = purchased),
    /// so it must have one more entry than <see cref="Costs"/>.
    /// Interpretation is upgrade-specific - see SpaceLeechSystem for usage.
    /// </summary>
    [DataField]
    public List<float> Magnitudes = new();

    /// <summary>Highest purchasable rank, derived from the cost list.</summary>
    public int MaxRank => Costs.Count;
}
