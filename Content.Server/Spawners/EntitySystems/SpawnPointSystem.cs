using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
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
        // TODO: Cache all this if it ends up important.
        var points = EntityQuery<SpawnPointComponent>().ToList();
        _random.Shuffle(points);
        foreach (var spawnPoint in points)
        {
            var xform = Transform(spawnPoint.Owner);
            if (args.Station != null && _stationSystem.GetOwningStation(spawnPoint.Owner, xform) != args.Station)
                continue;

            if (_gameTicker.RunLevel == GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.LateJoin)
            {
                args.SpawnResult = _stationSpawning.SpawnPlayerMob(
                    xform.Coordinates,
                    args.Job,
                    args.HumanoidCharacterProfile,
                    args.Station);

                return;
            }
            else if (_gameTicker.RunLevel != GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.Job && (args.Job == null || spawnPoint.Job?.ID == args.Job.Prototype.ID))
            {
                args.SpawnResult = _stationSpawning.SpawnPlayerMob(
                    xform.Coordinates,
                    args.Job,
                    args.HumanoidCharacterProfile,
                    args.Station);

                return;
            }
        }

        // Ok we've still not returned, but we need to put them /somewhere/.
        // TODO: Refactor gameticker spawning code so we don't have to do this!
        foreach (var spawnPoint in points)
        {
            var xform = Transform(spawnPoint.Owner);
            args.SpawnResult = _stationSpawning.SpawnPlayerMob(
                xform.Coordinates,
                args.Job,
                args.HumanoidCharacterProfile,
                args.Station);

            return;
        }

        Logger.ErrorS("spawning", "No spawn points were available!");
    }
}
