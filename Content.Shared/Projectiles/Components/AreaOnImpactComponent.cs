using Content.Shared.Damage;
using Content.Shared.Explosion;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles.Components;

[RegisterComponent]
public sealed partial class AreaOnImpactComponent : Component
{
    // Radius in tiles for the area effect.
    [DataField("tileRadius")] public int TileRadius = 1;

    // Optional direct damage to apply to entities in the area. If set, this damage will be applied directly
    // and no explosion prototype will be spawned.
    [DataField("damage")] public DamageSpecifier? Damage;

    // Whether to ignore resistances when applying the direct damage
    [DataField("ignoreResistances")] public bool IgnoreResistances = false;

    // Optional explosion prototype to spawn at impact. If set, the explosion system will be used instead
    // of directly applying damage.
    [DataField("explosionPrototype")] public ProtoId<ExplosionPrototype>? ExplosionPrototype;

    // Parameters used when converting tile radius to explosion intensity. Only used if ExplosionPrototype is set.
    [DataField("intensitySlope")] public float IntensitySlope = 1f;
    [DataField("maxTileIntensity")] public float MaxTileIntensity = 0f;

    // Optional tile break scale / max tile break values when using explosions. If left as default, the
    // explosion prototype's defaults will be used by the explosion system when queued.
    [DataField("tileBreakScale")] public float TileBreakScale = 1f;
    [DataField("maxTileBreak")] public int MaxTileBreak = int.MaxValue;
}
