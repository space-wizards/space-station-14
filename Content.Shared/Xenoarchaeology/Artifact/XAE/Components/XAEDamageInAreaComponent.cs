using Content.Shared.Damage;
using Content.Shared.Whitelist;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// When activated, damages nearby entities.
/// </summary>
[RegisterComponent, Access(typeof(XAEDamageInAreaSystem))]
public sealed partial class XAEDamageInAreaComponent : Component
{
    /// <summary>
    /// The radius of entities that will be affected
    /// </summary>
    [DataField]
    public float Radius = 3f;

    /// <summary>
    /// A whitelist for filtering certain damage.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The damage that is applied
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// The chance that damage is applied to each individual entity
    /// </summary>
    [DataField]
    public float DamageChance = 1f;

    /// <summary>
    /// Whether or not this should ignore resistances for the damage
    /// </summary>
    [DataField]
    public bool IgnoreResistances;
}
