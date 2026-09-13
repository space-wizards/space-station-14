using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Fluids.Components;

namespace Content.Server.EntityEffects.Effects.Solution;

/// <summary>
/// Spills the entity's solution onto the ground.
/// Will first try to use the solution from a <see cref="SpillableComponent"/> if present,
/// otherwise falls back to the solution specified in the effect.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpillEntityEffectSystem : EntityEffectSystem<SpillableComponent, Spill>
{
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SpillableComponent> entity, ref EntityEffectEvent<Spill> args)
    {
        var coordinates = Transform(entity).Coordinates;

        // Spill the solution that was drained/split
        if (args.Effect.Solution != null &&
            _solutionContainer.TryGetSolution(entity.Owner, args.Effect.Solution, out _, out var solution))
        {
            _puddle.TrySplashSpillAt(entity.Owner, coordinates, solution, out _, false, args.User);
        }
        else
        {
            _puddle.TrySplashSpillAt(entity.Owner, coordinates, out _, out _, false, args.User);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Spill : EntityEffectBase<Spill>
{
    /// <summary>
    /// Optional fallback solution name if <see cref="SpillableComponent"/> is not present.
    /// </summary>
    [DataField]
    public string? Solution;
}
