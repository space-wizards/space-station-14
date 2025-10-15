using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Atmos;

namespace Content.Server.EntityEffects.Effects.Atmos;

/// <summary>
/// Sets this entity on fire.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class IngiteEntityEffectSystem : EntityEffectSystem<FlammableComponent, Ignite>
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    protected override void Effect(Entity<FlammableComponent> entity, ref EntityEffectEvent<Ignite> args)
    {
        // TODO: Proper BodySystem Metabolism Effect relay...
        // TODO: If this fucks over downstream shitmed, I give you full approval to use whatever shitcode method you need to fix it. Metabolism is awful.
        _flammable.Ignite(entity, entity, flammable: entity.Comp);
    }
}

