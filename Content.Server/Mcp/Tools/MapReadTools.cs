#if !FULL_RELEASE || MCP
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Mcp.Tools;

public sealed class ListMapsTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "list_maps";

    public override string Description =>
        "Lists all maps: numeric map id, name, whether the map is initialized (post-mapinit) and paused, " +
        "and its grids. Maps being edited for saving must stay uninitialized and paused.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject());

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var maps = new JsonArray();
            foreach (var mapId in mapSystem.GetAllMapIds().OrderBy(m => m.GetHashCode()))
            {
                var grids = new JsonArray();
                foreach (var grid in mapSystem.GetAllGrids(mapId))
                {
                    grids.Add(Ctx.ToNetId(grid.Owner));
                }

                var entry = new JsonObject
                {
                    ["map_id"] = int.Parse(mapId.ToString()),
                    ["initialized"] = mapSystem.IsInitialized(mapId),
                    ["paused"] = mapSystem.IsPaused(mapId),
                    ["grids"] = grids,
                };

                if (mapSystem.TryGetMap(mapId, out var mapUid))
                {
                    entry["map_entity"] = Ctx.ToNetId(mapUid.Value);
                    entry["name"] = Ctx.EntityManager.GetComponent<MetaDataComponent>(mapUid.Value).EntityName;
                }

                maps.Add(entry);
            }

            return new JsonObject { ["maps"] = maps };
        });
    }
}

public sealed class ListGridsTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "list_grids";

    public override string Description =>
        "Lists grids (station structures) with their NetEntity id, name, map, world position, tile bounds and " +
        "tile count. Grid ids are what read_tiles / set_tiles and console commands expect.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["map"] = Schema.Int("Only list grids on this map id (default: all maps)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapFilter = McpContext.OptInt(args, "map");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
            var grids = new JsonArray();

            var mapIds = mapFilter is { } m
                ? new[] { new MapId(m) }
                : mapSystem.GetAllMapIds().ToArray();

            foreach (var mapId in mapIds)
            {
                foreach (var grid in mapSystem.GetAllGrids(mapId))
                {
                    var uid = grid.Owner;
                    var tileCount = 0;
                    var enumerator = mapSystem.GetAllTiles(uid, grid.Comp);
                    while (enumerator.MoveNext(out _))
                    {
                        tileCount++;
                    }

                    var worldPos = transformSystem.GetWorldPosition(uid);
                    var bounds = grid.Comp.LocalAABB;
                    grids.Add(new JsonObject
                    {
                        ["grid"] = Ctx.ToNetId(uid),
                        ["name"] = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid).EntityName,
                        ["map_id"] = int.Parse(mapId.ToString()),
                        ["world_x"] = MathF.Round(worldPos.X, 1),
                        ["world_y"] = MathF.Round(worldPos.Y, 1),
                        ["world_rotation_deg"] = Math.Round(transformSystem.GetWorldRotation(uid).Degrees, 1),
                        ["tile_bounds"] = $"({(int) bounds.Left},{(int) bounds.Bottom})..({(int) bounds.Right},{(int) bounds.Top})",
                        ["tile_count"] = tileCount,
                    });
                }
            }

            return new JsonObject { ["grids"] = grids };
        });
    }
}

public sealed class ReadTilesTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;

    public override string Name => "read_tiles";

    public override string Description =>
        "Reads a rectangle of tiles from a grid as a character matrix with a legend (tile prototype ids). " +
        "World-aligned: top row = north, left column = west. In absolute form x,y is the SOUTH-WEST corner; " +
        "with 'relative' the player-offset tile is the CENTER of the rectangle.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["grid"] = Schema.Grid(),
        ["x"] = Schema.Int("South-west corner tile X (absolute form)."),
        ["y"] = Schema.Int("South-west corner tile Y (absolute form)."),
        ["relative"] = Schema.Relative("the center of the rectangle"),
        ["width"] = Schema.Int("Rectangle width in tiles (default 21)."),
        ["height"] = Schema.Int("Rectangle height in tiles (default 21)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var width = McpContext.OptInt(args, "width") ?? 21;
        var height = McpContext.OptInt(args, "height") ?? 21;
        if (width < 1 || height < 1 || (long) width * height > MaxArea)
            throw new McpToolException($"width/height must be positive with area <= {MaxArea}; split larger reads.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var (gridUid, grid, anchor) = Ctx.ResolveTilePosition(args);
            var corner = args.ContainsKey("relative")
                ? anchor - new Vector2i(width / 2, height / 2)
                : anchor;

            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var legend = new McpLegend();
            var rows = new JsonArray();

            for (var y = corner.Y + height - 1; y >= corner.Y; y--)
            {
                var row = new StringBuilder(width);
                for (var x = corner.X; x < corner.X + width; x++)
                {
                    var tileRef = mapSystem.GetTileRef(gridUid, grid, new Vector2i(x, y));
                    row.Append(tileRef.Tile.IsEmpty ? '.' : legend.Get(TileTools.TileName(Ctx, tileRef.Tile.TypeId)));
                }

                rows.Add(row.ToString());
            }

            return new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["south_west_corner"] = new JsonObject { ["x"] = corner.X, ["y"] = corner.Y },
                ["width"] = width,
                ["height"] = height,
                ["orientation"] = "world-aligned: top row = north, left column = west",
                ["legend"] = legend.ToJson("empty (space)"),
                ["rows"] = rows,
            };
        });
    }
}

public sealed class FindTilesTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "find_tiles";

    public override string Description =>
        "Finds all tiles of the given tile prototype ids on a grid and returns their coordinates.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["grid"] = Schema.Grid(),
            ["tiles"] = Schema.Array("Tile prototype ids to look for (e.g. [\"FloorSteel\"]).", Schema.String("Tile prototype id.")),
            ["limit"] = Schema.Int("Max matches to return (default 500)."),
        },
        "tiles");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var limit = McpContext.OptInt(args, "limit") ?? 500;
        if (args["tiles"] is not JsonArray tilesArg || tilesArg.Count == 0)
            throw new McpToolException("'tiles' must be a non-empty array of tile prototype ids.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var targetIds = new HashSet<int>();
            foreach (var node in tilesArg)
            {
                var name = node?.GetValue<string>() ?? throw new McpToolException("'tiles' entries must be strings.");
                targetIds.Add(TileTools.ResolveTileDef(Ctx, name).TileId);
            }

            var (gridUid, grid) = Ctx.ResolveGrid(args);
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var matches = new JsonArray();
            var truncated = false;

            var enumerator = mapSystem.GetAllTiles(gridUid, grid);
            while (enumerator.MoveNext(out var tileRef))
            {
                if (!targetIds.Contains(tileRef.Value.Tile.TypeId))
                    continue;
                if (matches.Count >= limit)
                {
                    truncated = true;
                    break;
                }

                matches.Add(new JsonObject
                {
                    ["x"] = tileRef.Value.GridIndices.X,
                    ["y"] = tileRef.Value.GridIndices.Y,
                    ["tile"] = TileTools.TileName(Ctx, tileRef.Value.Tile.TypeId),
                });
            }

            return new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["matches"] = matches,
                ["truncated"] = truncated,
            };
        });
    }
}

/// <summary>Tile prototype helpers shared by tools.</summary>
public static class TileTools
{
    public static string TileName(McpContext ctx, int typeId)
    {
        var def = ctx.TileDefinitionManager[typeId];
        return def is ContentTileDefinition content ? content.ID : def.Name;
    }

    public static ContentTileDefinition ResolveTileDef(McpContext ctx, string id)
    {
        if (ctx.PrototypeManager.TryIndex<ContentTileDefinition>(id, out var def))
            return def;
        throw new McpToolException($"Unknown tile prototype '{id}' (see list_tile_prototypes).");
    }
}
#endif
