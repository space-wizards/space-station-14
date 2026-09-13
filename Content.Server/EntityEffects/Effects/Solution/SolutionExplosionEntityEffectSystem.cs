using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;

namespace Content.Server.EntityEffects.Effects.Solution;

/// <summary>
/// Spills the entity's solution and triggers its explosive component,
/// scaling the explosion by the remaining solution volume.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SolutionExplosionEntityEffectSystem : EntityEffectSystem<ExplosiveComponent, SolutionExplosion>
{
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private SharedExplosionSystem _explosion = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<ExplosiveComponent> entity, ref EntityEffectEvent<SolutionExplosion> args)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, args.Effect.Solution, out _, out var explodingSolution))
            return;

        // Don't explode if there's no solution
        if (explodingSolution.Volume == 0)
            return;

        // Scale the explosion intensity based on the remaining volume of solution
        var explosionScaleFactor = explodingSolution.FillFraction;

        // TODO: Perhaps some of the liquid should be discarded as if it's being consumed by the explosion

        // Spill the solution out into the world
        // Spill before exploding in anticipation of a future where the explosion can light the solution on fire.
        var coordinates = Transform(entity).Coordinates;
        _puddle.TrySpillAt(coordinates, explodingSolution, out _);

        // Explode
        // Don't delete the object here - let other processes like physical damage from the
        // explosion clean up the exploding object(s)
        var explosiveTotalIntensity = entity.Comp.TotalIntensity * explosionScaleFactor;
        _explosion.TriggerExplosive(entity, entity.Comp, false, explosiveTotalIntensity, user: args.User);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SolutionExplosion : EntityEffectBase<SolutionExplosion>
{
    /// <summary>
    /// The name of the solution to spill and scale the explosion by.
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;
}
