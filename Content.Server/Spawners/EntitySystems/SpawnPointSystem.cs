using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Prometheus;
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

    #region Starlight
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private static readonly ProtoId<SpeciesPrototype> FallbackSpecies = "Human";
    private static readonly ProtoId<JobPrototype> FallbackJob = "Assistant";
    private static readonly Histogram _speciesJobsSpawns = Metrics.CreateHistogram(
        "sl_species_jobs_spawns",
        "Contains info on species and jobs spawned at and during the round.",
        ["species", "job", "spawn_time"]
    );
    #endregion

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        // TODO: Cache all this if it ends up important.
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            if (_gameTicker.RunLevel == GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.LateJoin)
            {
                possiblePositions.Add(xform.Coordinates);
            }

            if (_gameTicker.RunLevel != GameRunLevel.InRound &&
                spawnPoint.SpawnType == SpawnPointType.Job &&
                (args.Job == null || spawnPoint.Job == args.Job))
            {
                possiblePositions.Add(xform.Coordinates);
            }
        }

        //starlight start, nukie spawn fix
        if (possiblePositions.Count == 0)
        {
            //so we havent found a valid spawn point
            //try to use a late joiner spawn point exclusively
            //this will most likely always end up being arrivals
            points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while ( points.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                if (spawnPoint.SpawnType == SpawnPointType.LateJoin)
                {
                    possiblePositions.Add(xform.Coordinates);
                }
            }
        }
        //starlight end

        if (possiblePositions.Count == 0)
        {
            // Ok we've still not returned, but we need to put them /somewhere/.
            // TODO: Refactor gameticker spawning code so we don't have to do this!
            var points2 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();

            if (points2.MoveNext(out var spawnPoint, out var xform))
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

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);

        #region StarlightStats
        if (args.SpawnResult != null)
        {
            if (!_prototypeManager.TryIndex(args.HumanoidCharacterProfile?.Species, out SpeciesPrototype? speciesProto))
            {
                speciesProto = _prototypeManager.Index(FallbackSpecies);
                Log.Warning($"Unable to find species {args.HumanoidCharacterProfile?.Species}, falling back to {FallbackSpecies}");
            }

            if (args.Job == null || !_prototypeManager.TryIndex(args.Job, out JobPrototype? jobProto))
            {
                jobProto = _prototypeManager.Index(FallbackJob);
                Log.Warning($"Unable to find job {args.Job}, falling back to {FallbackJob}");
            }

            _speciesJobsSpawns
                .WithLabels(
                    speciesProto.Name,
                    jobProto.Name,
                    _gameTicker.RunLevel.ToString())
                .Observe(1);
        }
        #endregion
    }
}
