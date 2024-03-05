using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Maps;

/// <summary>
/// Loads every map and resaves it into the data folder.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class ResaveCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public override string Command => "resave";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var loader = _entManager.System<MapLoaderSystem>();

        foreach (var fn in _res.ContentFindFiles(new ResPath("/Maps/")))
        {
            var mapId = _mapManager.CreateMap();
            _mapManager.AddUninitializedMap(mapId);
            loader.Load(mapId, fn.ToString(), new MapLoadOptions()
            {
                StoreMapUids = true,
                LoadMap = true,
            });

            // Process deferred component removals.
            _entManager.CullRemovedComponents();

            var mapUid = _mapManager.GetMapEntityId(mapId);
            var mapXform = _entManager.GetComponent<TransformComponent>(mapUid);

            if (_entManager.HasComponent<LoadedMapComponent>(mapUid) || mapXform.ChildCount != 1)
            {
                loader.SaveMap(mapId, fn.ToString());
            }
            else if (mapXform.ChildEnumerator.MoveNext(out var child))
            {
                loader.Save(child, fn.ToString());
            }

            _mapManager.DeleteMap(mapId);
        }
    }
}
