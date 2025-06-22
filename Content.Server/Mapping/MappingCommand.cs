using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Mapping
{
    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    public sealed class MappingCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IResourceManager _resourceMgr = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly MappingSystem _mappingSystem = default!;
        [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

        public override string Command => "mapping";

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-mapping-id"));
                case 2:
                    var opts = CompletionHelper.UserFilePath(args[1], _resourceMgr.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[1], _resourceMgr));
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-mapping-path"));
                case 3:
                    return CompletionResult.FromHintOptions(["false", "true"], Loc.GetString("cmd-mapping-hint-grid"));
            }
            return CompletionResult.Empty;
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (args.Length > 3)
            {
                shell.WriteLine(Help);
                return;
            }

#if DEBUG
            shell.WriteLine(Loc.GetString("cmd-mapping-warning"));
#endif

            // For backwards compatibility, isGrid is optional and we allow mappers to try load grids without explicitly
            // specifying that they are loading a grid. Currently content is not allowed to override a map's MapId, so
            // without engine changes this needs to be done by brute force by just trying to load it as a map first.
            // This can result in errors being logged if the file is actually a grid, but the command should still work.
            // yipeeee
            bool? isGrid = args.Length < 3 ? null : bool.Parse(args[2]);

            MapId mapId;
            string? toLoad = null;

            Entity<MapGridComponent>? grid = null;

            // Get the map ID to use
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out var intMapId))
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-failure-integer", ("arg", args[0])));
                    return;
                }

                mapId = new MapId(intMapId);

                // no loading null space
                if (mapId == MapId.Nullspace)
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-nullspace"));
                    return;
                }

                if (_mapSystem.MapExists(mapId))
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-exists", ("mapId", mapId)));
                    return;
                }

                // either load a map or create a new one.
                if (args.Length <= 1)
                {
                    _mapSystem.CreateMap(mapId, runMapInit: false);
                }
                else
                {
                    var path = new ResPath(args[1]);
                    toLoad = path.FilenameWithoutExtension;
                    var opts = new DeserializationOptions {StoreYamlUids = true};

                    if (isGrid == true)
                    {
                        _mapSystem.CreateMap(mapId, runMapInit: false);
                        if (!_mapLoader.TryLoadGrid(mapId, path, out grid, opts))
                        {
                            shell.WriteError(Loc.GetString("cmd-mapping-error"));
                            _mapSystem.DeleteMap(mapId);
                            return;
                        }
                    }
                    else if (!_mapLoader.TryLoadMapWithId(mapId, path, out _, out _, opts))
                    {
                        if (isGrid == false)
                        {
                            shell.WriteError(Loc.GetString("cmd-mapping-error"));
                            return;
                        }

                        // isGrid was not specified and loading it as a map failed, so we fall back to trying to load
                        // the file as a grid
                        shell.WriteLine(Loc.GetString("cmd-mapping-try-grid"));
                        _mapSystem.CreateMap(mapId, runMapInit: false);
                        if (!_mapLoader.TryLoadGrid(mapId, path, out grid, opts))
                        {
                            shell.WriteError(Loc.GetString("cmd-mapping-error"));
                            _mapSystem.DeleteMap(mapId);
                            return;
                        }
                    }
                }

                // was the map actually created or did it fail somehow?
                if (!_mapSystem.MapExists(mapId))
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-error"));
                    return;
                }
            }
            else
                _mapSystem.CreateMap(out mapId, runMapInit: false);

            // map successfully created. run misc helpful mapping commands
            if (player.AttachedEntity is { Valid: true } playerEntity &&
                EntityManager.GetComponent<MetaDataComponent>(playerEntity).EntityPrototype?.ID != GameTicker.AdminObserverPrototypeName)
            {
                shell.ExecuteCommand("aghost");
            }

            // don't interrupt mapping with events or auto-shuttle
            shell.ExecuteCommand("changecvar events.enabled false");
            shell.ExecuteCommand("changecvar shuttle.auto_call_time 0");

            if (grid != null)
                _mappingSystem.ToggleAutosave(grid.Value.Owner, toLoad ?? "NEWGRID");
            else
                _mappingSystem.ToggleAutosave(mapId, toLoad ?? "NEWMAP");

            shell.ExecuteCommand($"tp 0 0 {mapId}");
            shell.RemoteExecuteCommand("mappingclientsidesetup");
            DebugTools.Assert(_mapSystem.IsPaused(mapId));

            if (args.Length != 2)
                shell.WriteLine(Loc.GetString("cmd-mapping-success", ("mapId", mapId)));
            else if (grid == null)
                shell.WriteLine(Loc.GetString("cmd-mapping-success-load", ("mapId", mapId), ("path", args[1])));
            else
                shell.WriteLine(Loc.GetString("cmd-mapping-success-load-grid", ("mapId", mapId), ("path", args[1])));
        }
    }
}
