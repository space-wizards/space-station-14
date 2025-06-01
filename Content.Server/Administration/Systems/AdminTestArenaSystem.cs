using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

/// <summary>
/// This handles the administrative test arena maps, and loading them.
/// </summary>
public sealed class AdminTestArenaSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public const string ArenaMapPath = "/Maps/Test/admin_test_arena.yml";

    public Dictionary<NetUserId, EntityUid> ArenaMap { get; private set; } = new();
    public Dictionary<NetUserId, EntityUid?> ArenaGrid { get; private set; } = new();

    public (EntityUid Map, EntityUid? Grid) AssertArenaLoaded(ICommonSession admin)
    {
        if (ArenaMap.TryGetValue(admin.UserId, out var arenaMap) && !Deleted(arenaMap) && !Terminating(arenaMap))
        {
            if (ArenaGrid.TryGetValue(admin.UserId, out var arenaGrid) && !Deleted(arenaGrid) && !Terminating(arenaGrid.Value))
            {
                return (arenaMap, arenaGrid);
            }


            ArenaGrid[admin.UserId] = null;
            return (arenaMap, null);
        }

        var path = new ResPath(ArenaMapPath);
        var mapUid = _maps.CreateMap(out var mapId);

        if (!_loader.TryLoadGrid(mapId, path, out var grid))
        {
            QueueDel(mapUid);
            throw new Exception($"Failed to load admin arena");
        }

        ArenaMap[admin.UserId] = mapUid;
        _metaDataSystem.SetEntityName(mapUid, $"ATAM-{admin.Name}");

        ArenaGrid[admin.UserId] = grid.Value.Owner;
        _metaDataSystem.SetEntityName(grid.Value.Owner, $"ATAG-{admin.Name}");

        return (mapUid, grid.Value.Owner);
    }
}
