using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Containers;

namespace Content.Shared.WaterBalloon;

public sealed class WaterBalloonSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WaterBalloonComponent, SolutionContainerChangedEvent>(CheckIfFull);
    }

    /// <summary>
    /// Checks if the balloon has reached its MaxVolume value 
    /// </summary>
    private void CheckIfFull(Entity<WaterBalloonComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (_gameTiming.IsFirstTimePredicted  //prevents bad prediction shenanigans
            && args.Solution.MaxVolume == args.Solution.Volume
            && args.Solution.Name != null)
        {
            TieBalloon(ent, args.Solution, args.Solution.Name);  //send the entity solution and its name

        }
    }

    /// <summary>
    /// Ties the balloon (deletes the entity and spawns a new one with the same solution and color)
    /// </summary>
    private void TieBalloon(Entity<WaterBalloonComponent> ent, Solution balloonSolution, string solutionName)
    {
        //keeps the balloon ghost away
        if (!_net.IsServer)
            return;

        //drop empty balloon in the floor if its inside a container
        if (_containerSystem.IsEntityInContainer(ent))
        {
            _containerSystem.TryRemoveFromContainer(ent);
        }

        var coords = Transform(ent).Coordinates;
        EntityUid spawnedBalloon = EntityManager.SpawnEntity(ent.Comp.FilledPrototype, coords);

        //fill the new balloon with the "empty" balloon solution
        if (!_solutions.TryGetSolution(spawnedBalloon, solutionName, out var balloonContainer))
            return;
        _solutions.TryAddSolution(balloonContainer.Value, balloonSolution);

        EntityManager.QueueDeleteEntity(ent);
    }
}
