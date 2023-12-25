using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

public sealed class SpawnPointSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnSpawnPlayer);
    }

    private void OnSpawnPlayer(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        var possiblePositions = new List<EntityCoordinates>();

        Dictionary<ProtoId<JobPrototype>, List<EntityCoordinates>> jobSpawnsDict = new();
        List<EntityCoordinates> lateJoinSpawnsList = new();
        if (TryComp<StationSpawningComponent>(args.Station, out var stationSpawning))
        {
            jobSpawnsDict = stationSpawning.JobSpawnPoints;
            lateJoinSpawnsList = stationSpawning.LateJoinSpawnPoints;
        }

        if (_gameTicker.RunLevel == GameRunLevel.InRound && lateJoinSpawnsList is { Count: > 0 })
        {
            possiblePositions.AddRange(lateJoinSpawnsList);
        }
        else if (args.Job?.Prototype != null
            && jobSpawnsDict.TryGetValue((ProtoId<JobPrototype>) args.Job.Prototype, out var coordinatesList))
        {
                possiblePositions.AddRange(coordinatesList);
        }
        else
        {
            var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();

            while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                    continue;

                if (spawnPoint.Job != null)
                {
                    var spawnPointJobProto = new ProtoId<JobPrototype>(spawnPoint.Job.ID);
                    if (jobSpawnsDict.TryGetValue(spawnPointJobProto, out var coordsList))
                        coordsList.Add(xform.Coordinates);
                    else
                        jobSpawnsDict.Add(spawnPointJobProto, new List<EntityCoordinates>() { xform.Coordinates });
                }

                if (spawnPoint.SpawnType == SpawnPointType.LateJoin)
                {
                    lateJoinSpawnsList.Add(xform.Coordinates);
                    if (_gameTicker.RunLevel == GameRunLevel.InRound)
                        possiblePositions.Add(xform.Coordinates);
                }
                else if (_gameTicker.RunLevel != GameRunLevel.InRound &&
                         spawnPoint.SpawnType == SpawnPointType.Job &&
                         (args.Job == null || spawnPoint.Job?.ID == args.Job.Prototype))
                {
                    possiblePositions.Add(xform.Coordinates);
                }
            }
        }

        if (possiblePositions.Count == 0)
        {
            // Ok we've still not returned, but we need to put them /somewhere/.
            // TODO: Refactor gameticker spawning code so we don't have to do this!
            var points2 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();

            if (points2.MoveNext(out _, out _, out var xform))
            {
                possiblePositions.Add(xform.Coordinates);
            }
            else
            {
                Log.Error("No spawn points were available!");
                return;
            }
        }

        var spawnLoc = _random.Pick(possiblePositions);

        if (stationSpawning != null)
        {
            // Remove spawnpoint from pool unless it's the last one for this job.
            if (args.Job?.Prototype != null
                && jobSpawnsDict.TryGetValue((ProtoId<JobPrototype>) args.Job.Prototype, out var currentJobSpawns)
                && currentJobSpawns.Count > 1)
            {
                currentJobSpawns.Remove(spawnLoc);
            }

            stationSpawning.JobSpawnPoints = jobSpawnsDict;
            stationSpawning.LateJoinSpawnPoints = lateJoinSpawnsList;
        }

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }
}
