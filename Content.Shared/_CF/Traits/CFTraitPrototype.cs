using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Traits;

/// <summary>
/// A Pathfinder trait. Ids match the names used on spells and items, such as Agile or Fire.
/// Summaries are short mechanical notes, not source-book text.
/// </summary>
[Prototype("cfTrait")]
public sealed partial class CFTraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = "";

    [DataField]
    public CFTraitCategory Category;

    [DataField]
    public string Summary { get; private set; } = "";
}

public enum CFTraitCategory
{
    Common,
    Affliction,
    Alignment,
    Ancestry,
    Armor,
    Class,
    ClassSpecific,
    CreatureType,
    Elemental,
    Energy,
    Equipment,
    Feat,
    Hazard,
    Mechanic,
    Monster,
    Planar,
    Poison,
    Rarity,
    School,
    Sense,
    Settlement,
    Shield,
    Tradition,
    Weapon,
}
