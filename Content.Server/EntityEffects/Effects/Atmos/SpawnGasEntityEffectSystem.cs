using Content.Server.Atmos.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Atmos;

namespace Content.Server.EntityEffects.Effects.Atmos;

/// <summary>
/// Merges a gas mixture into the entity's environment.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpawnGasEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnGas>
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnGas> args)
    {
        var air = _atmosphere.GetContainingMixture(entity.AsNullable(), false, true);
        if (air != null)
            _atmosphere.Merge(air, args.Effect.Gas);
    }
}
