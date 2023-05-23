using Content.Server.Spawners.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;

namespace Content.Server.Spawners.EntitySystems;

/// <summary>
/// Handles the shuttle spawner component at mapinit
/// </summary>
public sealed class ShuttleSpawnerSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleSpawnerComponent, MapInitEvent>(OnMapInit);

        _sawmill = Logger.GetSawmill("shuttlespawner");
    }

    private void OnMapInit(EntityUid uid, ShuttleSpawnerComponent comp, MapInitEvent args)
    {
        // delete the spawner after its job is done
        QueueDel(uid);

        if (comp.Path == "")
        {
            _sawmill.Warning($"Initialized spawner {uid} with no shuttle path");
            return;
        }

        // build load options using the spawner transform
        var xform = Transform(uid);
        var options = new MapLoadOptions();
        options.Offset = xform.LocalPosition;
        options.Rotation = xform.LocalRotation;

        // spawn the shuttle
        if (!_mapLoader.TryLoad(xform.MapID, comp.Path, out var roots, options))
        {
            _sawmill.Error($"Failed to load shuttle {comp.Path} for spawner {uid}");
            return;
        }

        // TODO: implement automatic docking using comp.Airlocks
    }
}
