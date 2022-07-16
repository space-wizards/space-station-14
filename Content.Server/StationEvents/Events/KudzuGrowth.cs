using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class KudzuGrowth : StationEventSystem
{
    public override string Prototype => "KudzuGrowth";

    private EntityUid _targetGrid;
    private Vector2i _targetTile;
    private EntityCoordinates _targetCoords;

    public override void Started()
    {
        base.Started();

        // Pick a place to plant the kudzu.
        if (TryFindRandomTile(out _targetTile, out _, out _targetGrid, out _targetCoords))
        {
            EntityManager.SpawnEntity("Kudzu", _targetCoords);
            Sawmill.Info($"Spawning a Kudzu at {_targetTile} on {_targetGrid}");
        }

        // If the kudzu tile selection fails we just let the announcement happen anyways because it's funny and people
        // will be hunting the non-existent, dangerous plant.
    }
}
