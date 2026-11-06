using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Traits;

/// <summary>
/// A heritable plant trait, defined in YAML.
/// Plants store the IDs of their traits, so traits can be moved between plants by mutations and cross-pollination.
/// </summary>
[Prototype]
public sealed partial class PlantTraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized name of the trait.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Localized text shown when examining or analyzing a plant or produce that has this trait.
    /// </summary>
    [DataField]
    public LocId? Description;

    /// <summary>
    /// Effects applied to each produce harvested from a plant with this trait,
    /// and to produce that spawns outside of a harvest if its plant species has this trait by default.
    /// </summary>
    [DataField]
    public List<EntityEffect> ProduceEffects = [];

    /// <summary>
    /// If true, cross-pollination can neither add this trait to a plant nor remove it from one.
    /// </summary>
    [DataField]
    public bool PollinationProtected;
}
