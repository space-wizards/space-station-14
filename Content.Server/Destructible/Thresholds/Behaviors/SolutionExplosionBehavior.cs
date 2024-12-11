using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Explosion.Components;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    /// Works like a SpillBehavior combined with an ExplodeBehavior
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SolutionExplosionBehavior : IThresholdBehavior
    {
        [DataField(required: true)]
        public string Solution = default!;

        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            if (entManager.System<SharedSolutionContainerSystem>().TryGetSolution(owner, Solution, out _, out var explodingSolution)
                && entManager.TryGetComponent(owner, out ExplosiveComponent? explosiveComponent))
            {
                // Don't explode if there's no solution
                if (explodingSolution.Volume == 0)
                    return;

                // Scale the explosion intensity based on the remaining volume of solution
                var explosionScaleFactor = explodingSolution.FillFraction;

                // TODO: Perhaps some of the liquid should be discarded as if it's being consumed by the explosion

                // Spill the solution out into the world
                // Spill before exploding in anticipation of a future where the explosion can light the solution on fire.
                var coordinates = entManager.GetComponent<TransformComponent>(owner).Coordinates;
                entManager.System<PuddleSystem>().TrySpillAt(coordinates, explodingSolution, out _);

                // Explode
                // Don't delete the object here - let other processes like physical damage from the
                // explosion clean up the exploding object(s)
                var explosiveTotalIntensity = explosiveComponent.TotalIntensity * explosionScaleFactor;
                entManager.System<ExplosionSystem>().TriggerExplosive(owner, explosiveComponent, false, explosiveTotalIntensity, user:cause);
            }
        }
    }
}
