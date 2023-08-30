using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;

namespace Content.Server.Administration.Systems;

/// <summary>
/// This handles the administrative test arena maps, and loading them.
/// </summary>
public sealed class AdminTestArenaSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

    public const string ArenaMapPath = "/Maps/Test/admin_test_arena.yml";

    public Dictionary<NetUserId, EntityUid> ArenaMap { get; private set; } = new();
    public Dictionary<NetUserId, EntityUid?> ArenaGrid { get; private set; } = new();

    public (EntityUid Map, EntityUid? Grid) AssertArenaLoaded(IPlayerSession admin)
    {
        if (ArenaMap.TryGetValue(admin.UserId, out var arenaMap) && !Deleted(arenaMap) && !Terminating(arenaMap))
        {
            if (ArenaGrid.TryGetValue(admin.UserId, out var arenaGrid) && !Deleted(arenaGrid) && !Terminating(arenaGrid.Value))
            {
                return (arenaMap, arenaGrid);
            }
            else
            {
                ArenaGrid[admin.UserId] = null;
                return (arenaMap, null);
            }
        }

        ArenaMap[admin.UserId] = _mapManager.GetMapEntityId(_mapManager.CreateMap());
        _metaDataSystem.SetEntityName(ArenaMap[admin.UserId], $"ATAM-{admin.Name}");
        var grids = _map.LoadMap(Comp<MapComponent>(ArenaMap[admin.UserId]).MapId, ArenaMapPath);
        if (grids.Count != 0)
        {
            _metaDataSystem.SetEntityName(grids[0], $"ATAG-{admin.Name}");
            ArenaGrid[admin.UserId] = grids[0];
        }
        else
        {
            ArenaGrid[admin.UserId] = null;
        }

        return (ArenaMap[admin.UserId], ArenaGrid[admin.UserId]);
    }
}
