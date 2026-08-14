#if !FULL_RELEASE || MCP
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Shared.Ghost.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Mcp.Tools;

public sealed class PlayerStatusTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "player_status";

    public override string Description =>
        "Where each connected player is and how their screen is oriented: map, grid, world position, tile, " +
        "screen rotation (which world direction is at the top of their screen), zoom and ghost state. " +
        "Call this first to anchor all further work around the user.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject());

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var players = new JsonArray();
            foreach (var session in Ctx.PlayerManager.Sessions)
            {
                var entry = new JsonObject { ["name"] = session.Name };
                players.Add(entry);

                if (session.AttachedEntity is not { } entity || !Ctx.EntityManager.EntityExists(entity))
                {
                    entry["attached"] = false;
                    continue;
                }

                McpContext.PlayerFrame frame;
                try
                {
                    frame = Ctx.GetPlayerFrame(session.Name);
                }
                catch (McpToolException e)
                {
                    entry["attached"] = false;
                    entry["note"] = e.Message;
                    continue;
                }

                entry["attached"] = true;
                entry["entity"] = Ctx.ToNetId(entity);
                entry["prototype"] = Ctx.EntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype?.ID;
                entry["is_ghost"] = Ctx.EntityManager.HasComponent<GhostComponent>(entity);
                entry["map"] = frame.MapId.ToString();
                entry["grid"] = frame.GridUid is { } grid ? Ctx.ToNetId(grid) : null;
                entry["world_x"] = MathF.Round(frame.WorldPos.X, 2);
                entry["world_y"] = MathF.Round(frame.WorldPos.Y, 2);
                if (frame.TilePos is { } tile)
                {
                    entry["tile_x"] = tile.X;
                    entry["tile_y"] = tile.Y;
                }

                entry["screen_rotation_deg"] = Math.Round(frame.EyeRotation.Degrees, 1);
                entry["screen_up_points"] = McpContext.Compass(frame.ScreenUp);
                entry["screen_right_points"] = McpContext.Compass(frame.ScreenRight);
                if (Ctx.EntityManager.TryGetComponent<ContentEyeComponent>(entity, out var eye))
                    entry["zoom"] = MathF.Round(eye.TargetZoom.X, 2);
            }

            return new JsonObject { ["players"] = players };
        });
    }
}

public sealed class TeleportPlayerTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "teleport_player";

    public override string Description =>
        "Teleports a player: to a tile on a grid (absolute x/y or relative to their current position, " +
        "including in their screen frame — 'relative': {dx, dy, frame: \"screen\"}), or to world coordinates " +
        "on a map (map + world_x/world_y; lands on a grid there if one exists). Useful for moving your " +
        "admin-ghost around while surveying a map.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["player"] = Schema.String("Player name (default: the only connected player)."),
        ["grid"] = Schema.Grid(),
        ["x"] = Schema.Int("Target tile X (absolute form)."),
        ["y"] = Schema.Int("Target tile Y (absolute form)."),
        ["relative"] = Schema.Relative("the destination tile"),
        ["map"] = Schema.Int("Map id (world-coordinates form)."),
        ["world_x"] = Schema.Number("World X (world-coordinates form)."),
        ["world_y"] = Schema.Number("World Y (world-coordinates form)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapArg = McpContext.OptInt(args, "map");
        var worldX = McpContext.OptDouble(args, "world_x");
        var worldY = McpContext.OptDouble(args, "world_y");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var session = Ctx.ResolveSession(McpContext.OptString(args, "player"));
            if (session.AttachedEntity is not { } entity || !Ctx.EntityManager.EntityExists(entity))
                throw new McpToolException($"Player '{session.Name}' has no attached entity.");

            var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();

            EntityCoordinates target;
            if (mapArg is { } mapIdRaw && worldX is { } wx && worldY is { } wy)
            {
                var mapId = new MapId(mapIdRaw);
                if (!mapSystem.TryGetMap(mapId, out var mapUid))
                    throw new McpToolException($"Map {mapIdRaw} does not exist.");

                var worldPos = new Vector2((float) wx, (float) wy);
                // Land on the grid at that point, if any — same behavior as the tp command.
                if (mapSystem.TryFindGridAt(mapId, worldPos, out var gridUid, out var grid))
                    target = new EntityCoordinates(gridUid, mapSystem.WorldToLocal(gridUid, grid, worldPos));
                else
                    target = new EntityCoordinates(mapUid.Value, worldPos);
            }
            else
            {
                var (gridUid, grid, tile) = Ctx.ResolveTilePosition(args);
                target = mapSystem.GridTileToLocal(gridUid, grid, tile);
            }

            transformSystem.SetCoordinates(entity, target);

            var frame = Ctx.GetPlayerFrame(session.Name);
            return (JsonNode) new JsonObject
            {
                ["player"] = session.Name,
                ["map"] = frame.MapId.ToString(),
                ["grid"] = frame.GridUid is { } g ? Ctx.ToNetId(g) : null,
                ["tile_x"] = frame.TilePos?.X,
                ["tile_y"] = frame.TilePos?.Y,
                ["world_x"] = MathF.Round(frame.WorldPos.X, 2),
                ["world_y"] = MathF.Round(frame.WorldPos.Y, 2),
            };
        });
    }
}

public sealed class LookAroundTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxRadius = 32;

    public override string Name => "look_around";

    public override string Description =>
        "Snapshot of the area around a player, oriented the way the PLAYER SEES IT: character matrices of tiles " +
        "and anchored entities (top row = top of the player's screen, player marked '@'), plus nearby " +
        "unanchored entities and decal count. This is the main tool for working where the user is. " +
        "CAUTION: an entity-matrix cell shows only the tile's highest-priority anchored entity — everything it " +
        "hides is returned in 'stacked' (\"x,y\" -> prototypes, WORLD tile coords), cables/pipes in the separate " +
        "'subfloor' matrix, and the player's own tile stack in 'under_player'.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["player"] = Schema.String("Player name (default: the only connected player)."),
        ["radius"] = Schema.Int($"Half-size of the square in tiles, 1-{MaxRadius} (default 12)."),
        ["format"] = Schema.Enum(
            "Entity-matrix cell format: 'compact' (default, one char per tile) or 'wide' (two chars per cell, " +
            "space-separated: entity symbol + digit count of hidden entities, '.' if none).",
            "compact", "wide"),
        ["layers"] = Schema.Array(
            "Entity-matrix layers to return: 'main' (walls, doors, furniture...) and/or 'subfloor' " +
            "(cables, pipes). Default: both.",
            Schema.Enum("Layer name.", "main", "subfloor")),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var radius = McpContext.OptInt(args, "radius") ?? 12;
        if (radius is < 1 or > MaxRadius)
            throw new McpToolException($"radius must be between 1 and {MaxRadius}.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var frame = Ctx.GetPlayerFrame(McpContext.OptString(args, "player"));
            var result = new JsonObject
            {
                ["player"] = frame.Session.Name,
                ["map"] = frame.MapId.ToString(),
                ["grid"] = frame.GridUid is { } g ? Ctx.ToNetId(g) : null,
                ["player_tile"] = frame.TilePos is { } t ? new JsonObject { ["x"] = t.X, ["y"] = t.Y } : null,
                ["screen_up_points"] = McpContext.Compass(frame.ScreenUp),
                ["screen_right_points"] = McpContext.Compass(frame.ScreenRight),
                ["matrix_note"] =
                    "Matrices are in SCREEN space: top row = top of the player's screen, left column = left of " +
                    "their screen. '@' = player, '.' = empty, ' ' = off-grid. Use relative:{frame:'screen'} " +
                    "offsets to edit what you see here.",
            };

            if (frame.GridUid is not { } gridUid ||
                frame.TilePos is not { } playerTile ||
                !Ctx.EntityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
            {
                result["note"] = "Player is not standing on a grid; only nearby entities are listed.";
                result["entities"] = NearbyEntities(frame, radius, out _);
                return result;
            }

            var wide = (McpContext.OptString(args, "format") ?? "compact") == "wide";
            var (wantMain, wantSub) = McpEntityMatrix.ParseLayers(args);
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var tileLegend = new McpLegend();
            var entityLegend = new McpLegend();
            var subLegend = new McpLegend();
            var tileRows = new JsonArray();
            var entityRows = new JsonArray();
            var subRows = new JsonArray();
            var stacked = new JsonObject();
            var anySub = false;
            JsonArray? underPlayer = null;

            for (var sy = radius; sy >= -radius; sy--)
            {
                var tileRow = new StringBuilder();
                var entityRow = new StringBuilder();
                var subRow = new StringBuilder();
                for (var sx = -radius; sx <= radius; sx++)
                {
                    // Rotate the screen-space offset into world space so the matrix matches the screen.
                    var offset = frame.EyeRotation.RotateVec(new Vector2(sx, sy));
                    var tile = playerTile + new Vector2i((int) MathF.Round(offset.X), (int) MathF.Round(offset.Y));

                    var tileRef = mapSystem.GetTileRef(gridUid, grid, tile);
                    tileRow.Append(tileRef.Tile.IsEmpty ? '.' : tileLegend.Get(TileName(tileRef.Tile.TypeId)));

                    var cell = McpEntityMatrix.Collect(Ctx.EntityManager, mapSystem, gridUid, grid, tile);

                    if (tile == playerTile)
                    {
                        McpEntityMatrix.AppendCell(entityRow, '@', 0, wide);
                        McpEntityMatrix.AppendCell(subRow, '@', 0, wide);
                        if (!cell.IsEmpty)
                        {
                            underPlayer = new JsonArray(
                                (cell.Main ?? []).Concat(cell.Sub ?? []).Select(p => (JsonNode) p).ToArray());
                        }

                        continue;
                    }

                    var mainHidden = wantMain && cell.Main is { Count: > 1 } ? cell.Main.Count - 1 : 0;
                    var subHidden = wantSub && cell.Sub is { Count: > 1 } ? cell.Sub.Count - 1 : 0;
                    McpEntityMatrix.AddStacked(stacked, tile, cell, wantMain, wantSub);

                    if (wantMain)
                    {
                        McpEntityMatrix.AppendCell(entityRow,
                            cell.Main is { Count: > 0 } ? entityLegend.Get(cell.Main[0]) : '.', mainHidden, wide);
                    }

                    if (wantSub)
                    {
                        anySub |= cell.Sub is { Count: > 0 };
                        McpEntityMatrix.AppendCell(subRow,
                            cell.Sub is { Count: > 0 } ? subLegend.Get(cell.Sub[0]) : '.', subHidden, wide);
                    }
                }

                tileRows.Add(tileRow.ToString());
                if (wantMain)
                    entityRows.Add(entityRow.ToString());
                if (wantSub)
                    subRows.Add(subRow.ToString());
            }

            result["tiles"] = new JsonObject
            {
                ["legend"] = tileLegend.ToJson("empty (space)"),
                ["rows"] = tileRows,
            };
            if (wantMain)
            {
                result["anchored_entities"] = new JsonObject
                {
                    ["legend"] = entityLegend.ToJson("no anchored entity"),
                    ["rows"] = entityRows,
                };
            }

            if (wantSub && anySub)
            {
                result["subfloor"] = new JsonObject
                {
                    ["legend"] = subLegend.ToJson("no subfloor entity"),
                    ["rows"] = subRows,
                };
            }

            if (stacked.Count > 0)
                result["stacked"] = stacked;
            if (underPlayer != null)
                result["under_player"] = underPlayer;

            result["entities"] = NearbyEntities(frame, radius, out var truncated);
            if (truncated)
                result["entities_truncated"] = true;

            // Decals within the radius.
            var decalSystem = Ctx.EntityManager.System<Content.Server.Decals.DecalSystem>();
            var localPlayer = mapSystem.WorldToLocal(gridUid, grid, frame.WorldPos);
            var decals = decalSystem.GetDecalsInRange(gridUid, localPlayer, radius);
            result["decal_count"] = decals.Count;

            return result;
        });
    }

    private string TileName(int typeId)
    {
        var def = Ctx.TileDefinitionManager[typeId];
        return def is Content.Shared.Maps.ContentTileDefinition content ? content.ID : def.Name;
    }

    private JsonArray NearbyEntities(McpContext.PlayerFrame frame, int radius, out bool truncated)
    {
        const int maxGroups = 50;
        truncated = false;

        var lookup = Ctx.EntityManager.System<EntityLookupSystem>();
        var coords = Ctx.EntityManager.GetComponent<TransformComponent>(frame.Entity).Coordinates;
        var groups = new Dictionary<string, (int Count, EntityUid Sample)>();

        foreach (var uid in lookup.GetEntitiesInRange(coords, radius))
        {
            if (uid == frame.Entity)
                continue;
            var meta = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid);
            // Anchored entities are already visible in the matrix; list the loose ones.
            if (Ctx.EntityManager.GetComponent<TransformComponent>(uid).Anchored)
                continue;
            if (meta.EntityPrototype?.ID is not { } proto)
                continue;

            groups[proto] = groups.TryGetValue(proto, out var existing)
                ? (existing.Count + 1, existing.Sample)
                : (1, uid);
        }

        var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
        var array = new JsonArray();
        foreach (var (proto, (count, sample)) in groups.OrderByDescending(kv => kv.Value.Count))
        {
            if (array.Count >= maxGroups)
            {
                truncated = true;
                break;
            }

            var samplePos = transformSystem.GetWorldPosition(sample);
            var delta = samplePos - frame.WorldPos;
            array.Add(new JsonObject
            {
                ["prototype"] = proto,
                ["count"] = count,
                ["sample_entity"] = Ctx.ToNetId(sample),
                ["sample_from_player"] = $"{delta.Length():0.#} tiles {McpContext.Compass(delta)}",
            });
        }

        return array;
    }
}
#endif
