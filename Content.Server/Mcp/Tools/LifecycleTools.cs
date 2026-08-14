#if !FULL_RELEASE || MCP
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Mcp.Tools;

public sealed class CreateMapTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "create_map";

    public override string Description =>
        "Creates a new empty map, UNINITIALIZED and PAUSED (the state required for editing and saving maps). " +
        "Returns the new map id.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["map_id"] = Schema.Int("Specific map id to create (default: next free id)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var requestedId = McpContext.OptInt(args, "map_id");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            MapId mapId;
            EntityUid mapUid;
            if (requestedId is { } id)
            {
                mapId = new MapId(id);
                if (mapSystem.MapExists(mapId))
                    throw new McpToolException($"Map {id} already exists.");
                mapUid = mapSystem.CreateMap(mapId, runMapInit: false);
            }
            else
            {
                mapUid = mapSystem.CreateMap(out mapId, runMapInit: false);
            }

            return (JsonNode) new JsonObject
            {
                ["map_id"] = int.Parse(mapId.ToString()),
                ["map_entity"] = Ctx.ToNetId(mapUid),
                ["note"] = "Map is uninitialized and paused — keep it that way while editing for save.",
            };
        });
    }
}

public sealed class CreateGridTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "create_grid";

    public override string Description =>
        "Creates a new EMPTY grid on a map (a fresh structure to build on with set_tiles/spawn_entity). " +
        "A grid with no tiles left on it gets deleted automatically — place tiles right after creating it.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["map_id"] = Schema.Int("Map to create the grid on."),
            ["x"] = Schema.Number("World position X of the grid origin (default 0)."),
            ["y"] = Schema.Number("World position Y of the grid origin (default 0)."),
        },
        "map_id");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapId = new MapId(McpContext.GetInt(args, "map_id"));
        var x = (float) (McpContext.OptDouble(args, "x") ?? 0);
        var y = (float) (McpContext.OptDouble(args, "y") ?? 0);

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            if (!mapSystem.MapExists(mapId))
                throw new McpToolException($"Map {mapId} does not exist (create_map first).");

            var grid = mapSystem.CreateGridEntity(mapId);
            var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
            transformSystem.SetLocalPosition(grid.Owner, new System.Numerics.Vector2(x, y));

            return (JsonNode) new JsonObject
            {
                ["grid"] = Ctx.ToNetId(grid.Owner),
                ["note"] = "Grid is empty; set at least one tile now or it will be cleaned up.",
            };
        });
    }
}

public sealed class DeleteMapTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "delete_map";

    public override string Description =>
        "Deletes a map and EVERYTHING on it (all grids and entities). Irreversible — save first if needed.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["map_id"] = Schema.Int("Map id to delete."),
        },
        "map_id");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapId = new MapId(McpContext.GetInt(args, "map_id"));

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            if (!mapSystem.MapExists(mapId))
                throw new McpToolException($"Map {mapId} does not exist.");

            mapSystem.DeleteMap(mapId);
            return (JsonNode) new JsonObject { ["deleted"] = true };
        });
    }
}

public sealed class PauseMapTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "pause_map";

    public override string Description =>
        "Pauses or unpauses a map (paused = entities do not tick; maps under mapping should stay paused).";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["map_id"] = Schema.Int("Map id."),
            ["paused"] = Schema.Bool("true to pause, false to unpause."),
        },
        "map_id", "paused");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapId = new MapId(McpContext.GetInt(args, "map_id"));
        var paused = McpContext.OptBool(args, "paused", true);

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            if (!mapSystem.MapExists(mapId))
                throw new McpToolException($"Map {mapId} does not exist.");

            mapSystem.SetPaused(mapId, paused);
            return (JsonNode) new JsonObject { ["paused"] = paused };
        });
    }
}

public sealed class MapInitTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "map_init";

    public override string Description =>
        "Runs map initialization on a map (spawners fire, atmosphere/power start, map unpauses). " +
        "IRREVERSIBLE: an initialized map can no longer be cleanly saved as a map file. " +
        "Use only to playtest a copy, never on the map you are still editing.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["map_id"] = Schema.Int("Map id to initialize."),
        },
        "map_id");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapId = new MapId(McpContext.GetInt(args, "map_id"));

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            if (!mapSystem.MapExists(mapId))
                throw new McpToolException($"Map {mapId} does not exist.");
            if (mapSystem.IsInitialized(mapId))
                throw new McpToolException($"Map {mapId} is already initialized.");

            mapSystem.InitializeMap(mapId);
            return (JsonNode) new JsonObject { ["initialized"] = true };
        });
    }
}

public sealed class SaveMapTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "save_map";

    public override string Description =>
        "Saves a whole map to a YAML file in the server's user-data directory (e.g. path '/my_map.yml'). " +
        "Optionally also copies the result to an absolute filesystem path (e.g. into the repo's Resources/Maps). " +
        "Refuses to save initialized maps unless force=true.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["map_id"] = Schema.Int("Map id to save."),
            ["path"] = Schema.String("User-data path for the yml, must start with '/' (e.g. '/mymap.yml')."),
            ["force"] = Schema.Bool("Save even if the map is initialized (default false)."),
            ["export_path"] = Schema.String("Optional absolute path to copy the saved file to."),
        },
        "map_id", "path");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapId = new MapId(McpContext.GetInt(args, "map_id"));
        var path = McpContext.GetString(args, "path");
        var force = McpContext.OptBool(args, "force", false);
        var exportPath = McpContext.OptString(args, "export_path");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var loader = Ctx.EntityManager.System<MapLoaderSystem>();
            if (!mapSystem.MapExists(mapId))
                throw new McpToolException($"Map {mapId} does not exist.");
            if (mapSystem.IsInitialized(mapId) && !force)
                throw new McpToolException(
                    $"Map {mapId} is initialized; saving it will not produce a clean map file. Pass force=true to override.");

            var resPath = new ResPath(path).ToRootedPath();
            if (!loader.TrySaveMap(mapId, resPath))
                throw new McpToolException("Map save failed (see server log).");

            return (JsonNode) LifecycleTools.SaveResult(Ctx, resPath, exportPath);
        });
    }
}

public sealed class SaveGridTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "save_grid";

    public override string Description =>
        "Saves a single grid to a YAML file in the server's user-data directory (see also save_map). " +
        "Use for shuttles, inserts and other grid files.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["grid"] = Schema.Int("Grid NetEntity id to save."),
            ["path"] = Schema.String("User-data path for the yml, must start with '/' (e.g. '/mygrid.yml')."),
            ["export_path"] = Schema.String("Optional absolute path to copy the saved file to."),
        },
        "grid", "path");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var gridNet = McpContext.GetInt(args, "grid");
        var path = McpContext.GetString(args, "path");
        var exportPath = McpContext.OptString(args, "export_path");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var loader = Ctx.EntityManager.System<MapLoaderSystem>();
            var gridUid = Ctx.FromNetId(gridNet);
            var resPath = new ResPath(path).ToRootedPath();
            if (!loader.TrySaveGrid(gridUid, resPath))
                throw new McpToolException("Grid save failed (is the entity actually a grid?).");

            return (JsonNode) LifecycleTools.SaveResult(Ctx, resPath, exportPath);
        });
    }
}

public sealed class LoadMapTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "load_map";

    public override string Description =>
        "Loads a map YAML file onto a NEW map (uninitialized and paused, ready for editing). The path is " +
        "resolved against server resources (e.g. '/Maps/bagel.yml') or the user-data directory " +
        "(earlier save_map output). Yaml uids are preserved for stable diffs.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["path"] = Schema.String("Map file path, must start with '/'."),
            ["map_id"] = Schema.Int("Specific map id to load onto (default: next free id)."),
        },
        "path");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var path = McpContext.GetString(args, "path");
        var requestedId = McpContext.OptInt(args, "map_id");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var loader = Ctx.EntityManager.System<MapLoaderSystem>();
            var resPath = new ResPath(path).ToRootedPath();
            var options = new DeserializationOptions { StoreYamlUids = true };

            Robust.Shared.GameObjects.Entity<Robust.Shared.Map.Components.MapComponent>? map;
            HashSet<Robust.Shared.GameObjects.Entity<Robust.Shared.Map.Components.MapGridComponent>>? grids;
            var loaded = requestedId is { } id
                ? loader.TryLoadMapWithId(new MapId(id), resPath, out map, out grids, options)
                : loader.TryLoadMap(resPath, out map, out grids, options);

            if (!loaded || map == null)
                throw new McpToolException($"Failed to load '{path}' (wrong path, or the file is a grid — use load_grid).");

            var gridArray = new JsonArray();
            foreach (var grid in grids!)
            {
                gridArray.Add(Ctx.ToNetId(grid.Owner));
            }

            return (JsonNode) new JsonObject
            {
                ["map_id"] = int.Parse(map.Value.Comp.MapId.ToString()),
                ["map_entity"] = Ctx.ToNetId(map.Value.Owner),
                ["grids"] = gridArray,
            };
        });
    }
}

public sealed class LoadGridTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "load_grid";

    public override string Description =>
        "Loads a grid YAML file onto an EXISTING map at an optional offset/rotation. " +
        "Use for shuttles/inserts, or to compose maps from grid files.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["map_id"] = Schema.Int("Target map id."),
            ["path"] = Schema.String("Grid file path, must start with '/'."),
            ["x"] = Schema.Number("World offset X (default 0)."),
            ["y"] = Schema.Number("World offset Y (default 0)."),
            ["rotation_deg"] = Schema.Number("Rotation in degrees (default 0)."),
        },
        "map_id", "path");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapId = new MapId(McpContext.GetInt(args, "map_id"));
        var path = McpContext.GetString(args, "path");
        var x = (float) (McpContext.OptDouble(args, "x") ?? 0);
        var y = (float) (McpContext.OptDouble(args, "y") ?? 0);
        var rotation = Angle.FromDegrees(McpContext.OptDouble(args, "rotation_deg") ?? 0);

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            if (!mapSystem.MapExists(mapId))
                throw new McpToolException($"Map {mapId} does not exist (create_map first).");

            var loader = Ctx.EntityManager.System<MapLoaderSystem>();
            var options = new DeserializationOptions { StoreYamlUids = true };
            if (!loader.TryLoadGrid(mapId, new ResPath(path).ToRootedPath(), out var grid, options,
                    new System.Numerics.Vector2(x, y), rotation))
            {
                throw new McpToolException($"Failed to load grid '{path}'.");
            }

            return (JsonNode) new JsonObject { ["grid"] = Ctx.ToNetId(grid.Value.Owner) };
        });
    }
}

public sealed class SetAmbientLightTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "set_ambient_light";

    public override string Description =>
        "Sets a map's ambient light color (e.g. '#FFFFFF' full bright for editing, '#000000' darkness).";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["map_id"] = Schema.Int("Map id."),
            ["color"] = Schema.String("Hex color, e.g. '#404040'."),
        },
        "map_id", "color");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapId = new MapId(McpContext.GetInt(args, "map_id"));
        var colorHex = McpContext.GetString(args, "color");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            if (!Color.TryParse(colorHex, out var color))
                throw new McpToolException($"Cannot parse color '{colorHex}'.");

            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            if (!mapSystem.MapExists(mapId))
                throw new McpToolException($"Map {mapId} does not exist.");

            mapSystem.SetAmbientLight(mapId, color);
            return (JsonNode) new JsonObject { ["set"] = true };
        });
    }
}

public sealed class MappingSessionTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "mapping_session";

    public override string Description =>
        "Starts the full in-game mapping mode for a player, exactly like typing 'mapping' in their console: " +
        "creates/loads the map paused, admin-ghosts the player, disables events, teleports them there, enables " +
        "autosave and switches their client into the mapping editor UI. The player must be an admin " +
        "(local host players are by default).";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["player"] = Schema.String("Player name (default: the only connected player)."),
        ["map_id"] = Schema.Int("Map id to create or load onto (optional)."),
        ["path"] = Schema.String("Map/grid file to load, e.g. '/Maps/bagel.yml' (optional)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapId = McpContext.OptInt(args, "map_id");
        var path = McpContext.OptString(args, "path");
        if (path != null && mapId == null)
            throw new McpToolException("Loading a file requires an explicit map_id.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var session = Ctx.ResolveSession(McpContext.OptString(args, "player"));
            var command = "mapping";
            if (mapId is { } id)
                command += $" {id}";
            if (path != null)
                command += $" \"{path}\"";

            Ctx.ConsoleHost.ExecuteCommand(session, command);
            return (JsonNode) new JsonObject
            {
                ["executed"] = command,
                ["player"] = session.Name,
                ["note"] = "Check player_status / list_maps to confirm the mapping session started.",
            };
        });
    }
}

public static class LifecycleTools
{
    /// <summary>Builds the save result, optionally exporting the user-data file to an absolute path.</summary>
    public static JsonObject SaveResult(McpContext ctx, ResPath resPath, string? exportPath)
    {
        var result = new JsonObject
        {
            ["saved"] = true,
            ["user_data_path"] = resPath.ToString(),
        };

        if (exportPath == null)
            return result;

        if (!System.IO.Path.IsPathRooted(exportPath))
            throw new McpToolException("export_path must be an absolute filesystem path.");

        using var source = ctx.ResourceManager.UserData.OpenRead(resPath);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(exportPath)!);
        using var target = System.IO.File.Create(exportPath);
        source.CopyTo(target);
        result["exported_to"] = exportPath;
        return result;
    }
}
#endif
