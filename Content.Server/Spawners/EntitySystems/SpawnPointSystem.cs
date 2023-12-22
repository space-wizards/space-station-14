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

        TryComp<StationSpawningComponent>(args.Station, out var stationSpawning);
        var jobSpawnsDict = stationSpawning?.JobSpawnPoints;

        if (jobSpawnsDict is not null && args.Job?.Prototype != null)
        {
            if (jobSpawnsDict.TryGetValue((ProtoId<JobPrototype>) args.Job.Prototype, out var coordinatesList))
                possiblePositions.AddRange(coordinatesList);
        }
        else
        {
            var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            jobSpawnsDict = new ();

            while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                    continue;

                if (spawnPoint.Job is not null)
                {
                    var spawnPointJobProto = new ProtoId<JobPrototype>(spawnPoint.Job.ID);
                    if (jobSpawnsDict.TryGetValue(spawnPointJobProto, out var coordinatesList))
                        coordinatesList.Add(xform.Coordinates);
                    else
                        jobSpawnsDict.Add(spawnPointJobProto, new List<EntityCoordinates> { xform.Coordinates });
                }

                if (_gameTicker.RunLevel == GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.LateJoin)
                {
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

            if (points2.MoveNext(out var uid, out var spawnPoint, out var xform))
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

        if (args.Job?.Prototype != null)
            jobSpawnsDict[(ProtoId<JobPrototype>) args.Job.Prototype].Remove(spawnLoc);

        if (stationSpawning != null)
            stationSpawning.JobSpawnPoints = jobSpawnsDict;

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }
}
