using Content.Shared.Damage;
using Content.Shared.Whitelist;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

[RegisterComponent]
public sealed class DamageNearbyArtifactComponent : Component
{
    [DataField("radius")]
    public float Radius = 3f;

    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;

    [DataField("ignoreResistances")]
    public bool IgnoreResistances = false;
}
