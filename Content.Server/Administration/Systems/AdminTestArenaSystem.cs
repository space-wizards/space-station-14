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
        if (!_loader.TryLoadMap(path, out var map, out var grids))
            throw new Exception($"Failed to load admin arena");

        ArenaMap[admin.UserId] = map.Value.Owner;
        _metaDataSystem.SetEntityName(map.Value.Owner, $"ATAM-{admin.Name}");

        var grid = grids.FirstOrNull();
        ArenaGrid[admin.UserId] = grid?.Owner;
        if (grid != null)
            _metaDataSystem.SetEntityName(grid.Value.Owner, $"ATAG-{admin.Name}");

        return (map.Value.Owner, grid?.Owner);
    }
}
