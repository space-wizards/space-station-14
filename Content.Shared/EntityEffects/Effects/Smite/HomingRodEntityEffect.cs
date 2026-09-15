using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Smite;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HomingRod : EntityEffectBase<HomingRod>
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField(required: true)]
    public float Distance;

    [DataField(required: true)]
    public float Speed;

    /// <summary>
    /// Use the target's current sprint speed plus a small offset, falling back to Speed if unavailable.
    /// </summary>
    [DataField]
    public bool MatchTargetSprintSpeed;
}
