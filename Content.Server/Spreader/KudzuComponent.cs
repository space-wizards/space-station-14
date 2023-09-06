using Content.Shared.Damage;

namespace Content.Server.Spreader;

/// <summary>
/// Handles entities that spread out when they reach the relevant growth level.
/// </summary>
[RegisterComponent]
public sealed partial class KudzuComponent : Component
{
    /// <summary>
    /// Chance to spread whenever an edge spread is possible.
    /// </summary>
    [DataField("spreadChance")]
    public float SpreadChance = 1f;

    /// <summary>
    /// How much damage is required to reduce growth level
    /// </summary>
    [DataField("growthHealth")]
    public float GrowthHealth = 10.0f;

    /// <summary>
    /// How much damage is required to prevent growth
    /// </summary>
    [DataField("growthBlock")]
    public float GrowthBlock = 20.0f;

    /// <summary>
    /// How much the kudzu heals each tick
    /// </summary>
    [DataField("damageRecovery")]
    public DamageSpecifier? DamageRecovery = null;

    [DataField("growthTickChance")]
    public float GrowthTickChance = 1f;

}
