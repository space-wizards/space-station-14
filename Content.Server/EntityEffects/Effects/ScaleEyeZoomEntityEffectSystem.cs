using Content.Server.Movement.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Movement.Components;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Multiplies this entity's target eye zoom, ignoring normal zoom limits.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class ScaleEyeZoomEntityEffectSystem : EntityEffectSystem<MetaDataComponent, ScaleEyeZoom>
{
    [Dependency] private ContentEyeSystem _contentEye = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ScaleEyeZoom> args)
    {
        var eye = EnsureComp<ContentEyeComponent>(entity);
        _contentEye.SetZoom(entity, eye.TargetZoom * args.Effect.Factor, true, eye);
    }
}
