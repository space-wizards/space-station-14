#if !FULL_RELEASE || MCP
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server.Mcp.Tools;

public sealed class SpawnEntityTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxBatch = 500;

    public override string Name => "spawn_entity";

    public override string Description =>
        "Spawns one entity (or a batch) at a tile, snapped to the tile center — the sandbox spawn panel for agents. " +
        "Position is a tile on a grid (absolute x,y or relative to the player, including in screen frame). " +
        "Optionally sets facing and anchors/unanchors after spawning. Returns the new NetEntity ids.";

    public override JsonObject InputSchema
    {
        get
        {
            var single = new JsonObject
            {
                ["prototype"] = Schema.String("Entity prototype id (see list_entity_prototypes)."),
                ["x"] = Schema.Int("Tile X (absolute form)."),
                ["y"] = Schema.Int("Tile Y (absolute form)."),
                ["relative"] = Schema.Relative("the spawn tile"),
                ["facing"] = Schema.String("Facing: 'north'/'south'/'east'/'west' (or degrees as a number string)."),
                ["anchored"] = Schema.Bool("Override anchoring after spawn (default: prototype's own behavior)."),
            };
            var props = new JsonObject
            {
                ["grid"] = Schema.Grid(),
                ["entities"] = Schema.Array("Batch form: list of spawns (same fields as the single form).",
                    Schema.Object((JsonObject) single.DeepClone())),
            };
            foreach (var (key, value) in single)
            {
                props[key] = value!.DeepClone();
            }

            return Schema.Object(props);
        }
    }

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var spawned = new JsonArray();
            if (args.TryGetPropertyValue("entities", out var batchNode) && batchNode is JsonArray batch)
            {
                if (batch.Count > MaxBatch)
                    throw new McpToolException($"Batch too large ({batch.Count} > {MaxBatch}).");

                foreach (var node in batch)
                {
                    if (node is not JsonObject entry)
                        throw new McpToolException("'entities' entries must be objects.");
                    // Inherit the grid from the top-level arguments unless overridden.
                    if (!entry.ContainsKey("grid") && args.ContainsKey("grid"))
                        entry["grid"] = args["grid"]!.DeepClone();
                    spawned.Add(SpawnOne(entry));
                }
            }
            else
            {
                spawned.Add(SpawnOne(args));
            }

            return (JsonNode) new JsonObject { ["spawned"] = spawned };
        });
    }

    private JsonObject SpawnOne(JsonObject args)
    {
        var prototype = McpContext.GetString(args, "prototype");
        if (!Ctx.PrototypeManager.HasIndex<Robust.Shared.Prototypes.EntityPrototype>(prototype))
            throw new McpToolException($"Unknown entity prototype '{prototype}' (see list_entity_prototypes).");

        var (gridUid, grid, tile) = Ctx.ResolveTilePosition(args);
        var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
        var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
        var coords = mapSystem.GridTileToLocal(gridUid, grid, tile);

        var uid = Ctx.EntityManager.SpawnEntity(prototype, coords);

        if (ParseFacing(args) is { } angle)
            transformSystem.SetLocalRotation(uid, angle);

        var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);
        if (args.TryGetPropertyValue("anchored", out _) &&
            McpContext.OptBool(args, "anchored", xform.Anchored) is var anchored && anchored != xform.Anchored)
        {
            if (anchored)
                transformSystem.AnchorEntity((uid, xform));
            else
                transformSystem.Unanchor(uid, xform);
        }

        return new JsonObject
        {
            ["entity"] = Ctx.ToNetId(uid),
            ["prototype"] = prototype,
            ["x"] = tile.X,
            ["y"] = tile.Y,
            ["anchored"] = xform.Anchored,
        };
    }

    internal static Angle? ParseFacing(JsonObject args)
    {
        if (McpContext.OptString(args, "facing") is not { } facing)
            return null;

        if (double.TryParse(facing, out var degrees))
            return Angle.FromDegrees(degrees);

        return facing.ToLowerInvariant() switch
        {
            "south" => Angle.FromDegrees(0),
            "east" => Angle.FromDegrees(90),
            "north" => Angle.FromDegrees(180),
            "west" => Angle.FromDegrees(270),
            _ => throw new McpToolException("facing must be north/south/east/west or a number in degrees."),
        };
    }
}

public sealed class DeleteEntitiesTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxListed = 100;

    public override string Name => "delete_entities";

    public override string Description =>
        "Deletes entities by NetEntity ids, or every entity matching filters inside a rectangle. " +
        "Never deletes grids, maps or player-controlled entities. Deleting an area WITHOUT any filter removes " +
        "all prototype-spawned entities there (like the eraser), so filter when you only mean one kind.";

    public override JsonObject InputSchema
    {
        get
        {
            var props = new JsonObject
            {
                ["entities"] = Schema.Array("Explicit NetEntity ids to delete.", Schema.Int("NetEntity id.")),
                ["grid"] = Schema.Grid(),
                ["x"] = Schema.Int("Area south-west corner tile X."),
                ["y"] = Schema.Int("Area south-west corner tile Y."),
                ["relative"] = Schema.Relative("the area center"),
                ["width"] = Schema.Int("Area width in tiles."),
                ["height"] = Schema.Int("Area height in tiles."),
            };
            foreach (var (key, value) in EntityFilter.SchemaProperties())
            {
                props[key] = value!.DeepClone();
            }

            return Schema.Object(props);
        }
    }

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var toDelete = new List<EntityUid>();

            if (args.TryGetPropertyValue("entities", out var idsNode) && idsNode is JsonArray ids)
            {
                foreach (var node in ids)
                {
                    if (node is not JsonValue value || !value.TryGetValue<int>(out var netId))
                        throw new McpToolException("'entities' entries must be integers.");
                    toDelete.Add(Ctx.FromNetId(netId));
                }
            }
            else if (McpContext.OptInt(args, "width") is { } width && McpContext.OptInt(args, "height") is { } height)
            {
                if (width < 1 || height < 1)
                    throw new McpToolException("width/height must be positive.");

                var filter = EntityFilter.Parse(Ctx, args);
                var (gridUid, grid, anchor) = Ctx.ResolveTilePosition(args);
                var corner = args.ContainsKey("relative")
                    ? anchor - new Vector2i(width / 2, height / 2)
                    : anchor;

                var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
                var found = new HashSet<EntityUid>();

                // Anchored entities: walk the per-tile lists — exact tile coverage, no AABB slop.
                for (var ty = corner.Y; ty < corner.Y + height; ty++)
                {
                    for (var tx = corner.X; tx < corner.X + width; tx++)
                    {
                        var anchoredEnum = mapSystem.GetAnchoredEntities(gridUid, grid, new Vector2i(tx, ty));
                        while (anchoredEnum.MoveNext(out var anchoredUid))
                        {
                            found.Add(anchoredUid.Value);
                        }
                    }
                }

                // Loose entities: physics lookup, then keep only those whose tile is inside the
                // rectangle — the AABB query alone also returns entities merely touching its edge.
                var lookup = Ctx.EntityManager.System<EntityLookupSystem>();
                var intersecting = new HashSet<EntityUid>();
                lookup.GetLocalEntitiesIntersecting(gridUid,
                    new Box2(corner.X, corner.Y, corner.X + width, corner.Y + height), intersecting);
                foreach (var uid in intersecting)
                {
                    var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);
                    if (xform.Anchored)
                        continue;

                    var tile = mapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);
                    if (tile.X >= corner.X && tile.X < corner.X + width &&
                        tile.Y >= corner.Y && tile.Y < corner.Y + height)
                    {
                        found.Add(uid);
                    }
                }

                foreach (var uid in found)
                {
                    var meta = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid);
                    var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);
                    if (meta.EntityPrototype == null)
                        continue;
                    if (filter.Matches(Ctx, uid, meta, xform))
                        toDelete.Add(uid);
                }
            }
            else
            {
                throw new McpToolException("Provide 'entities' or an area (width+height with x/y or relative).");
            }

            var deleted = new JsonArray();
            var deletedCount = 0;
            foreach (var uid in toDelete)
            {
                if (!Ctx.EntityManager.EntityExists(uid))
                    continue;
                // Never delete grids, maps or player avatars this way.
                if (Ctx.EntityManager.HasComponent<MapGridComponent>(uid) ||
                    Ctx.EntityManager.HasComponent<MapComponent>(uid) ||
                    Ctx.EntityManager.HasComponent<ActorComponent>(uid))
                {
                    continue;
                }

                if (deleted.Count < MaxListed)
                {
                    deleted.Add(new JsonObject
                    {
                        ["entity"] = Ctx.ToNetId(uid),
                        ["prototype"] = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID,
                    });
                }

                Ctx.EntityManager.DeleteEntity(uid);
                deletedCount++;
            }

            return (JsonNode) new JsonObject
            {
                ["deleted_count"] = deletedCount,
                ["deleted"] = deleted,
            };
        });
    }
}

public sealed class TransformEntityTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "transform_entity";

    public override string Description =>
        "Moves, rotates, anchors or unanchors an existing entity. Position (if given) snaps to the tile center " +
        "of the target grid; rotation/anchoring can be changed independently.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["entity"] = Schema.Int("Entity NetEntity id."),
            ["grid"] = Schema.Grid(),
            ["x"] = Schema.Int("Target tile X (absolute form)."),
            ["y"] = Schema.Int("Target tile Y (absolute form)."),
            ["relative"] = Schema.Relative("the target tile"),
            ["facing"] = Schema.String("New facing: 'north'/'south'/'east'/'west' or degrees."),
            ["anchored"] = Schema.Bool("Anchor (true) or unanchor (false)."),
        },
        "entity");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var netId = McpContext.GetInt(args, "entity");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var uid = Ctx.FromNetId(netId);
            var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);

            var moved = false;
            if (args.ContainsKey("x") || args.ContainsKey("relative"))
            {
                var (gridUid, grid, tile) = Ctx.ResolveTilePosition(args);
                var wasAnchored = xform.Anchored;
                if (wasAnchored)
                    transformSystem.Unanchor(uid, xform);
                transformSystem.SetCoordinates(uid, mapSystem.GridTileToLocal(gridUid, grid, tile));
                if (wasAnchored)
                    transformSystem.AnchorEntity((uid, xform));
                moved = true;
            }

            if (SpawnEntityTool.ParseFacing(args) is { } angle)
                transformSystem.SetLocalRotation(uid, angle);

            if (args.ContainsKey("anchored"))
            {
                var anchored = McpContext.OptBool(args, "anchored", xform.Anchored);
                if (anchored && !xform.Anchored)
                {
                    if (!transformSystem.AnchorEntity((uid, xform)))
                        throw new McpToolException("Failed to anchor (no grid under the entity?).");
                }
                else if (!anchored && xform.Anchored)
                {
                    transformSystem.Unanchor(uid, xform);
                }
            }

            var meta = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid);
            var result = EntityTools.Describe(Ctx, uid, meta, xform);
            result["moved"] = moved;
            return (JsonNode) result;
        });
    }
}
#endif
