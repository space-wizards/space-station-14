using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// When activated, damages nearby entities.
/// </summary>
[RegisterComponent, Access(typeof(XAEDamageInAreaSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XAEDamageInAreaComponent : Component
{
    /// <summary>
    /// The radius of entities that will be affected
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Radius = 3f;

    /// <summary>
    /// A whitelist for filtering certain damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The damage that is applied
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// The chance that damage is applied to each individual entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageChance = 1f;

    /// <summary>
    /// Whether or not this should ignore resistances for the damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreResistances;
}
