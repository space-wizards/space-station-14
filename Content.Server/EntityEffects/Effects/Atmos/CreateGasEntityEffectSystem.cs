using Content.Server.Atmos.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Atmos;

namespace Content.Server.EntityEffects.Effects.Atmos;

public sealed partial class CreateGasEntityEffectSystem : EntityEffectSystem<TransformComponent, CreateGas>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<CreateGas> args)
    {
        var tileMix = _atmosphere.GetContainingMixture(entity.AsNullable(), false, true);

        tileMix?.AdjustMoles(args.Effect.Gas, args.Scale * args.Effect.Multiplier);
    }
}
