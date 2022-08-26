using System.Linq;
using Content.Server._00OuterRim.Worldgen.Components;
using Content.Server.Ghost.Components;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace Content.Server._00OuterRim.Worldgen.Systems;

public class PopulatorSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Update(float frameTime)
    {

        foreach (var (unpop, grid, transform) in EntityManager.EntityQuery<UnpopulatedComponent, IMapGridComponent, TransformComponent>())
        {
            // TODO: This could be much smarter with high player counts or large debris, but it's fine for now.
            var nearby = _playerManager.ServerSessions.Any(x =>
                x.AttachedEntity is not null && // Must have an entity.
                (x.AttachedEntityTransform?.WorldPosition - transform.WorldPosition)?.Length < 64 && // Must be withing 64 units of the unpopulated debris.
                !HasComp<GhostComponent>(x.AttachedEntity.Value)); // Must NOT be a ghost.

            if (nearby)
            {
                var startTime = _gameTiming.RealTime;
                unpop.Populator?.Populate(grid.Owner, grid.Grid);
                var timeSpan = _gameTiming.RealTime - startTime;
                Logger.InfoS("worldgen", $"Populated grid {grid.GridIndex} in {timeSpan.TotalMilliseconds:N2}ms.");
                RemComp<UnpopulatedComponent>(unpop.Owner);
            }

        }
    }
}
