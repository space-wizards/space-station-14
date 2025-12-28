using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Maps;

/// <summary>
/// Loads every map and resaves it into the data folder.
/// </summary>
[AdminCommand(AdminFlags.Host)]
public sealed class ResaveCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly ILogManager _log = default!;

    public override string Command => "resave";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var loader = _entManager.System<MapLoaderSystem>();

        var opts = MapLoadOptions.Default with
        {

            DeserializationOptions = DeserializationOptions.Default with
            {
                StoreYamlUids = true,
                LogOrphanedGrids = false
            }
        };

        var log = _log.GetSawmill(Command);
        var files = _res.ContentFindFiles(new ResPath("/Maps/")).ToList();

        for (var i = 0; i < files.Count; i++)
        {
            var fn = files[i];
            log.Info($"Re-saving file {i}/{files.Count} : {fn}");

            if (!loader.TryLoadGeneric(fn, out var result, opts))
                continue;

            if (result.Maps.Count != 1)
            {
                shell.WriteError(
                    Loc.GetString("cmd-resave-multi-map-or-grid", ("fn", fn), ("command", Command)));
                loader.Delete(result);
                continue;
            }

            var map = result.Maps.First();

            // Process deferred component removals.
            _entManager.CullRemovedComponents();

            if (_entManager.HasComponent<LoadedMapComponent>(map))
            {
                loader.TrySaveMap(map.Comp.MapId, fn);
            }
            else if (result.Grids.Count == 1)
            {
                loader.TrySaveGrid(result.Grids.First(), fn);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-resave-failed-to-resave", ("fn", fn)));
            }

            loader.Delete(result);
        }

        shell.WriteLine(Loc.GetString("cmd-resave-resaved-all-maps"));
    }
}
