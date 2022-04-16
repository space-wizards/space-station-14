using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;

namespace Content.Server.Spawners.EntitySystems;

public sealed class SpawnPointSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnPointComponent, SpawnPlayerEvent>(OnSpawnPlayer);
    }

    private void OnSpawnPlayer(EntityUid uid, SpawnPointComponent component, ref SpawnPlayerEvent args)
    {
        // Spawn the player here if the job matches, we're not in-round, and we're a job spawner OR if we're a late-join spawner.
        if (args.Job != null && args.Job?.Prototype.ID != component.Job?.ID && _gameTicker.RunLevel != GameRunLevel.InRound && component.SpawnType == SpawnPointType.Job)
            return;

        if (component.SpawnType is SpawnPointType.Observer or SpawnPointType.Unset)
            return;

        var xform = Transform(uid);
        args.SpawnResult = _stationSpawning.SpawnPlayerMob(xform.Coordinates, args.Job, args.HumanoidCharacterProfile);
    }
}
