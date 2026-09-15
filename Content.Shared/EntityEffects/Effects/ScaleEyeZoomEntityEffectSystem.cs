using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Multiplies this entity's target eye zoom, ignoring normal zoom limits.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class ScaleEyeZoomEntityEffectSystem : EntityEffectSystem<MetaDataComponent, ScaleEyeZoom>
{
    [Dependency] private SharedContentEyeSystem _contentEye = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ScaleEyeZoom> args)
    {
        var eye = EnsureComp<ContentEyeComponent>(entity);
        _contentEye.SetZoom(entity, eye.TargetZoom * args.Effect.Factor, true, eye);
    }
}
