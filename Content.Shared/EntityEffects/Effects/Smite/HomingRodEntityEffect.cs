using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Smite;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HomingRod : EntityEffectBase<HomingRod>
{
    /// <summary>
    /// Entity prototype of the rod to spawn.
    /// </summary>
    [DataField]
    public EntProtoId Prototype = "ImmovableRodKeepTilesStill";

    /// <summary>
    /// Distance from the target at which the rod spawns.
    /// </summary>
    [DataField]
    public float Distance = 200;

    /// <summary>
    /// Speed at which the rod chases the target.
    /// </summary>
    [DataField]
    public float Speed = 20;

    /// <summary>
    /// Use the target's current sprint speed plus a small offset, falling back to Speed if unavailable.
    /// </summary>
    [DataField]
    public bool MatchTargetSprintSpeed;
}
