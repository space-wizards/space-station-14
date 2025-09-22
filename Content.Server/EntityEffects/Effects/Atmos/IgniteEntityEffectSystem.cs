using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Atmos;

namespace Content.Server.EntityEffects.Effects.Atmos;

public sealed partial class IngiteEntityEffectSystem : SharedIgniteEntityEffectSystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    protected override void Effect(Entity<FlammableComponent> entity, ref EntityEffectEvent<Ingite> args)
    {
        // TODO: This no longer allows for you to set your organs on fire, but that didn't do anything anywas.
        // TODO: Metabolism just needs to properly relay effects to their organs for this to work.
        // TODO: If this fucks over downstream shitmed, I give you full approval to use whatever shitcode method you need to fix it. Metabolism is awful.
        _flammable.Ignite(entity, entity, flammable: entity.Comp);
    }
}

