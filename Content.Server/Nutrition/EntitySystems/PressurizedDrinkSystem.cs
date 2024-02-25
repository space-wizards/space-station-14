using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.EntitySystems;

public sealed partial class PressurizedSolutionSystem : SharedPressurizedSolutionSystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;

    // TODO: When more of PuddleSystem is in Shared, move this method from Server to Shared
    protected override void DoSpraySplash(Entity<PressurizedSolutionComponent> entity, Solution sol, EntityUid? user = null)
    {
        base.DoSpraySplash(entity, sol, user);

        _puddle.TrySpillAt(entity, sol, out _);
    }
}
