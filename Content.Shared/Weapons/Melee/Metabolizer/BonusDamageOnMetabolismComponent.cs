using Content.Shared.Damage;
using Content.Shared.Metabolism;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee.Metabolizer;

/// <summary>
/// Deals bonus damage when hitting an entity with the selected metabolizer
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BonusDamageOnMetabolismComponent : Component
{
    /// <summary>
    /// List of metabolizers that cannot be selected.
    /// </summary>
    [DataField]
    public List<ProtoId<MetabolizerTypePrototype>> ExcludedMetabolizers = new();

    /// <summary>
    /// The currently selected metabolizer, can be null for none.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<MetabolizerTypePrototype>? SelectedMetabolizer;

    /// <summary>
    /// Amount of extra damage to deal when hitting an entity with the SelectedMetabolizer metabolism
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// If true, will only do bonus damage if the mob is alive or critical, but not dead.
    /// </summary>
    [DataField]
    public bool OnlyWorksOnAlive = true;
}
