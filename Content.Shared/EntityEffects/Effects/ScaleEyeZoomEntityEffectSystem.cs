using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Multiplies this entity's target eye zoom, ignoring normal zoom limits.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class ScaleEyeZoomEntityEffectSystem : EntityEffectSystem<ContentEyeComponent, ScaleEyeZoom>
{
    [Dependency] private SharedContentEyeSystem _contentEye = default!;

    protected override void Effect(Entity<ContentEyeComponent> entity, ref EntityEffectEvent<ScaleEyeZoom> args)
    {
        _contentEye.SetZoom(entity, entity.Comp.TargetZoom * args.Effect.Factor, true, entity.Comp);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ScaleEyeZoom : EntityEffectBase<ScaleEyeZoom>
{
    /// <summary>
    /// Multiplier for the current target zoom. Values below one zoom in; must be positive and finite.
    /// </summary>
    [DataField(required: true)]
    public float Factor;
}
