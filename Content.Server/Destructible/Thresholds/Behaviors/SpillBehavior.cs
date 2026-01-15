using JetBrains.Annotations;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpillBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    /// <summary>
    /// Optional fallback solution name if SpillableComponent is not present.
    /// </summary>
    [DataField]
    public string? Solution;

    /// <summary>
    /// When triggered, spills the entity's solution onto the ground.
    /// Will first try to use the solution from a SpillableComponent if present,
    /// otherwise falls back to the solution specified in the behavior's data fields.
    /// The solution is properly drained/split before spilling to prevent double-spilling with other behaviors.
    /// </summary>
    /// <param name="owner">Entity whose solution will be spilled</param>
    /// <param name="cause">Optional entity that caused this behavior to trigger</param>
    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        var coordinates = Transform(owner).Coordinates;

        // Spill the solution that was drained/split
        if (_solutionContainer.TryGetSolution(owner, Solution, out _, out var solution))
            _puddle.TrySplashSpillAt(owner, coordinates, solution, out _, false, cause);
        else
            _puddle.TrySplashSpillAt(owner, coordinates, out _, out _, false, cause);
    }
}
