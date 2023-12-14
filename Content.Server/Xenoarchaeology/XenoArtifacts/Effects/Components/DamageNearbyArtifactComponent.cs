using Content.Shared.Damage;
using Content.Shared.Whitelist;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// When activated, damages nearby entities.
/// </summary>
[RegisterComponent]
public sealed partial class DamageNearbyArtifactComponent : Component
{
    /// <summary>
    /// The radius of entities that will be affected
    /// </summary>
    [DataField("radius")]
    public float Radius = 3f;

    /// <summary>
    /// A whitelist for filtering certain damage.
    /// </summary>
    /// <remarks>
    /// TODO: The component portion, since it uses an array, does not work currently.
    /// </remarks>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The damage that is applied
    /// </summary>
    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// The chance that damage is applied to each individual entity
    /// </summary>
    [DataField("damageChance")]
    public float DamageChance = 1f;

    /// <summary>
    /// Whether or not this should ignore resistances for the damage
    /// </summary>
    [DataField("ignoreResistances")]
    public bool IgnoreResistances;
}
