using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;

using Robust.Shared.Random;

namespace Content.Server._CD.Spawners;

public sealed class ArrivalsSpawnPointSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        // Ensure they have a job, so that we won't end up making mobs spawn on arrivals.
        if (args.JobId == null)
            return;

        var generalSpawns = new List<Entity<ArrivalsSpawnPointComponent>>();
        var jobSpawns = new List<Entity<ArrivalsSpawnPointComponent>>();

        var query = EntityQueryEnumerator<ArrivalsSpawnPointComponent>();
        while (query.MoveNext(out var spawnUid, out var spawnPoint))
        {
            if (spawnPoint.JobIds.Count == 0)
            {
                generalSpawns.Add((spawnUid, spawnPoint));
                continue;
            }

            jobSpawns.Add((spawnUid, spawnPoint));
        }

        _random.Shuffle(jobSpawns);

        foreach (var (spawnUid, spawnPoint) in jobSpawns)
        {
            foreach(var jobId in spawnPoint.JobIds!)
            {
                if (jobId == args.JobId)
                {

                    _transform.SetCoordinates(args.Mob, Transform(spawnUid).Coordinates);
                    return;
                }
            }
        }

        _random.Shuffle(generalSpawns);
        var spawn = generalSpawns.First();

        var xform = Transform(spawn);
        _transform.SetCoordinates(args.Mob, xform.Coordinates);
        return;
    }
}
