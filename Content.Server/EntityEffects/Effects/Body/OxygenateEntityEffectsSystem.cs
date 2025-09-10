using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.NewEffects.Body;

namespace Content.Server.EntityEffects.Effects.Body;

/// <summary>
/// This is used for...
/// </summary>
public sealed partial class OxygenateEntityEffectsSystem : EntityEffectSystem<RespiratorComponent, Oxygenate>
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    protected override void Effect(Entity<RespiratorComponent> entity, ref EntityEffectEvent<Oxygenate> args)
    {
        var multiplier = 1f;

        _respirator.UpdateSaturation(entity, multiplier * args.Effect.Factor, entity.Comp);
    }
}
