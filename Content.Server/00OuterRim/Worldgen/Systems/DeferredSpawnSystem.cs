using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._00OuterRim.Worldgen.Systems;

public class DeferredSpawnSystem : EntitySystem
{
    private readonly Stopwatch _simulationStopwatch = new();

    private Queue<(string, EntityCoordinates)> SpawnQueue = new();

    public override void Update(float frameTime)
    {
        _simulationStopwatch.Restart();
        while (SpawnQueue.Count > 0)
        {
            var dat = SpawnQueue.Dequeue();
            Spawn(dat.Item1, dat.Item2);
            if (_simulationStopwatch.Elapsed.Milliseconds > 5)
                break;
        }
    }

    public void SpawnEntityDeferred(string prototype, EntityCoordinates position)
    {
        SpawnQueue.Enqueue((prototype, position));
    }
}
