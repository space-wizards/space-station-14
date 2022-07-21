using System.Linq;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Server.Administration.Systems;

/// <summary>
/// This handles the administrative test arena maps, and loading them.
/// </summary>
public sealed class AdminTestArenaSystem : EntitySystem
{
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public const string ArenaMapPath = "/Maps/Test/admin_test_arena.yml";

    public Dictionary<NetUserId, EntityUid> ArenaMap { get; private set; } = new();
    public Dictionary<NetUserId, EntityUid> ArenaGrid { get; private set; } = new();

    public (EntityUid, EntityUid) AssertArenaLoaded(IPlayerSession admin)
    {
        if (ArenaMap.TryGetValue(admin.UserId, out var arenaMap) && !Deleted(arenaMap) && !Terminating(arenaMap))
            return (arenaMap, ArenaGrid[admin.UserId]);

        ArenaMap[admin.UserId] = _mapManager.GetMapEntityId(_mapManager.CreateMap());
        var (grids, _) = _mapLoader.LoadMap(Comp<MapComponent>(ArenaMap[admin.UserId]).WorldMap, ArenaMapPath);
        ArenaGrid[admin.UserId] = grids.First();

        return (ArenaMap[admin.UserId], ArenaGrid[admin.UserId]);
    }
}
