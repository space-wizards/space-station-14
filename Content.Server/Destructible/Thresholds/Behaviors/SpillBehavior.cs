using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids.Components;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpillBehavior : IThresholdBehavior
{
    /// <summary>
    /// When triggered, spills the entity's solution onto the ground.
    /// Will first try to use the solution from a SpillableComponent if present,
    /// otherwise falls back to the solution specified in the behavior's data fields.
    /// The solution is properly drained/split before spilling to prevent double-spilling with other behaviors.
    /// </summary>
    /// <param name="owner">Entity whose solution will be spilled</param>
    /// <param name="system">System calling this behavior</param>
    /// <param name="cause">Optional entity that caused this behavior to trigger</param>
    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        var spillableSystem = system.EntityManager.System<PuddleSystem>();
        var coordinates = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;

        // Spill the solution that was drained/split
        spillableSystem.TrySplashSpillAt(owner, coordinates, out _, out _, false, cause);
    }
}
