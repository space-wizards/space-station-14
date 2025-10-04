using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Atmos;

namespace Content.Server.EntityEffects.Effects.Atmos;

public sealed partial class FlammableEntityEffectSystem : EntityEffectSystem<FlammableComponent, Flammable>
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    protected override void Effect(Entity<FlammableComponent> entity, ref EntityEffectEvent<Flammable> args)
    {
        // Sets the multiplier for FireStacks to MultiplierOnExisting is 0 or greater and target already has FireStacks
        var multiplier = entity.Comp.FireStacks != 0f && args.Effect.MultiplierOnExisting >= 0 ? args.Effect.MultiplierOnExisting : args.Effect.Multiplier;

        _flammable.AdjustFireStacks(entity, args.Scale * multiplier, entity.Comp);
    }
}
