#if !FULL_RELEASE || MCP
using System.Numerics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Shared.Decals;
using Robust.Shared.Map;

namespace Content.Server.Mcp.Tools;

/// <summary>
/// Decal ids travel over JSON as the "chunkX,chunkY:id" string <see cref="DecalIndex"/> already
/// prints, so an id read by one tool can be pasted straight into another.
/// </summary>
public static class DecalId
{
    public const string Format = "'chunkX,chunkY:id', e.g. '0,0:3'";

    public static DecalIndex Parse(JsonObject args, string name)
    {
        var raw = McpContext.GetString(args, name);
        var colon = raw.IndexOf(':');
        var comma = raw.IndexOf(',');
        if (colon > 0 && comma > 0 && comma < colon &&
            int.TryParse(raw[..comma], out var cx) &&
            int.TryParse(raw[(comma + 1)..colon], out var cy) &&
            ushort.TryParse(raw[(colon + 1)..], out var id))
        {
            return new DecalIndex(new Vector2i(cx, cy), id);
        }

        throw new McpToolException($"Cannot parse decal id '{raw}'; expected {Format}.");
    }
}

public sealed class ReadDecalsTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;

    public override string Name => "read_decals";

    public override string Description =>
        "Lists decals (floor markings, dirt, stripes...) inside a rectangle of a grid, with their decal ids " +
        "(needed by edit_decal / remove_decal), position, color, rotation and z-index.";

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
            throw new McpToolException($"width/height must be positive with area <= {MaxArea}.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var (gridUid, _, anchor) = Ctx.ResolveTilePosition(args);
            var corner = args.ContainsKey("relative")
                ? anchor - new Vector2i(width / 2, height / 2)
                : anchor;

            var decalSystem = Ctx.EntityManager.System<DecalSystem>();
            var found = decalSystem.GetDecalsIntersecting(gridUid,
                new Box2(corner.X, corner.Y, corner.X + width, corner.Y + height));

            var decals = new JsonArray();
            foreach (var (index, decal) in found)
            {
                decals.Add(new JsonObject
                {
                    ["decal_id"] = index.ToString(),
                    ["prototype"] = decal.Id,
                    ["x"] = MathF.Round(decal.Coordinates.X, 2),
                    ["y"] = MathF.Round(decal.Coordinates.Y, 2),
                    ["color"] = decal.Color?.ToHex(),
                    ["rotation_deg"] = Math.Round(decal.Angle.Degrees, 1),
                    ["z_index"] = decal.ZIndex,
                    ["cleanable"] = decal.Cleanable,
                });
            }

            return (JsonNode) new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["decals"] = decals,
            };
        });
    }
}

public sealed class AddDecalTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "add_decal";

    public override string Description =>
        "Places a decal. By default it snaps to the tile at the given position (like the decal placer with snap " +
        "on); pass snap=false and fractional dx/dy offsets for free placement. Returns the decal id.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["decal"] = Schema.String("Decal prototype id (see list_decal_prototypes)."),
            ["grid"] = Schema.Grid(),
            ["x"] = Schema.Int("Tile X (absolute form)."),
            ["y"] = Schema.Int("Tile Y (absolute form)."),
            ["relative"] = Schema.Relative("the decal position"),
            ["snap"] = Schema.Bool("Snap to the tile (default true)."),
            ["color"] = Schema.String("Hex color like '#FFFFFF' (optional)."),
            ["rotation_deg"] = Schema.Number("Rotation in degrees (default 0)."),
            ["z_index"] = Schema.Int("Draw order among decals (default 0)."),
            ["cleanable"] = Schema.Bool("Can be cleaned with a mop (default false)."),
        },
        "decal");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var decalId = McpContext.GetString(args, "decal");
        var snap = McpContext.OptBool(args, "snap", true);
        var rotation = Angle.FromDegrees(McpContext.OptDouble(args, "rotation_deg") ?? 0);
        var zIndex = McpContext.OptInt(args, "z_index") ?? 0;
        var cleanable = McpContext.OptBool(args, "cleanable", false);
        var colorHex = McpContext.OptString(args, "color");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            if (!Ctx.PrototypeManager.HasIndex<DecalPrototype>(decalId))
                throw new McpToolException($"Unknown decal prototype '{decalId}' (see list_decal_prototypes).");

            Color? color = null;
            if (colorHex != null)
            {
                if (!Color.TryParse(colorHex, out var parsed))
                    throw new McpToolException($"Cannot parse color '{colorHex}'.");
                color = parsed;
            }

            var (gridUid, _, tile) = Ctx.ResolveTilePosition(args);
            // A decal's coordinates are its sprite's bottom-left corner; the tile corner covers the tile exactly.
            var position = snap
                ? new Vector2(tile.X, tile.Y)
                : new Vector2(tile.X + 0.5f, tile.Y + 0.5f);

            var decalSystem = Ctx.EntityManager.System<DecalSystem>();
            var coordinates = new EntityCoordinates(gridUid, position);
            if (!decalSystem.TryAddDecal(decalId, coordinates, out var newId, color, rotation, zIndex, cleanable))
                throw new McpToolException("Failed to place decal (is the target tile empty space?).");

            return (JsonNode) new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["decal_id"] = newId.ToString(),
            };
        });
    }
}

public sealed class EditDecalTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "edit_decal";

    public override string Description =>
        "Edits an existing decal (find its id with read_decals): position, color, rotation, z-index, cleanable " +
        "or the decal prototype itself.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["grid"] = Schema.Int("Grid NetEntity id the decal is on."),
            ["decal_id"] = Schema.String($"Decal id from read_decals or add_decal, {DecalId.Format}."),
            ["x"] = Schema.Number("New X (grid-local, tile units)."),
            ["y"] = Schema.Number("New Y (grid-local, tile units)."),
            ["decal"] = Schema.String("New decal prototype id."),
            ["color"] = Schema.String("New hex color."),
            ["rotation_deg"] = Schema.Number("New rotation in degrees."),
            ["z_index"] = Schema.Int("New z-index."),
            ["cleanable"] = Schema.Bool("New cleanable flag."),
        },
        "grid", "decal_id");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var gridNet = McpContext.GetInt(args, "grid");
        var decalId = DecalId.Parse(args, "decal_id");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var gridUid = Ctx.FromNetId(gridNet);
            var decalSystem = Ctx.EntityManager.System<DecalSystem>();
            var changed = new JsonArray();

            var x = McpContext.OptDouble(args, "x");
            var y = McpContext.OptDouble(args, "y");
            if (x != null || y != null)
            {
                if (x == null || y == null)
                    throw new McpToolException("Provide both x and y to move a decal.");
                if (!decalSystem.SetDecalPosition(gridUid, decalId,
                        new EntityCoordinates(gridUid, new Vector2((float) x.Value, (float) y.Value))))
                    throw new McpToolException($"No decal {decalId} on grid {gridNet}.");
                changed.Add("position");
            }

            if (McpContext.OptString(args, "decal") is { } newProto)
            {
                if (!Ctx.PrototypeManager.HasIndex<DecalPrototype>(newProto))
                    throw new McpToolException($"Unknown decal prototype '{newProto}'.");
                if (!decalSystem.SetDecalId(gridUid, decalId, newProto))
                    throw new McpToolException($"No decal {decalId} on grid {gridNet}.");
                changed.Add("decal");
            }

            if (McpContext.OptString(args, "color") is { } colorHex)
            {
                if (!Color.TryParse(colorHex, out var color))
                    throw new McpToolException($"Cannot parse color '{colorHex}'.");
                if (!decalSystem.SetDecalColor(gridUid, decalId, color))
                    throw new McpToolException($"No decal {decalId} on grid {gridNet}.");
                changed.Add("color");
            }

            if (McpContext.OptDouble(args, "rotation_deg") is { } degrees)
            {
                if (!decalSystem.SetDecalRotation(gridUid, decalId, Angle.FromDegrees(degrees)))
                    throw new McpToolException($"No decal {decalId} on grid {gridNet}.");
                changed.Add("rotation");
            }

            if (McpContext.OptInt(args, "z_index") is { } zIndex)
            {
                if (!decalSystem.SetDecalZIndex(gridUid, decalId, zIndex))
                    throw new McpToolException($"No decal {decalId} on grid {gridNet}.");
                changed.Add("z_index");
            }

            if (args.ContainsKey("cleanable"))
            {
                if (!decalSystem.SetDecalCleanable(gridUid, decalId, McpContext.OptBool(args, "cleanable", false)))
                    throw new McpToolException($"No decal {decalId} on grid {gridNet}.");
                changed.Add("cleanable");
            }

            if (changed.Count == 0)
                throw new McpToolException("Nothing to change; pass at least one property.");

            return (JsonNode) new JsonObject { ["changed"] = changed };
        });
    }
}

public sealed class RemoveDecalTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "remove_decal";

    public override string Description => "Removes a decal by id (find it with read_decals).";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["grid"] = Schema.Int("Grid NetEntity id the decal is on."),
            ["decal_id"] = Schema.String($"Decal id from read_decals, {DecalId.Format}."),
        },
        "grid", "decal_id");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var gridNet = McpContext.GetInt(args, "grid");
        var decalId = DecalId.Parse(args, "decal_id");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var gridUid = Ctx.FromNetId(gridNet);
            var decalSystem = Ctx.EntityManager.System<DecalSystem>();
            if (!decalSystem.RemoveDecal(gridUid, decalId))
                throw new McpToolException($"No decal {decalId} on grid {gridNet}.");

            return (JsonNode) new JsonObject { ["removed"] = true };
        });
    }
}
#endif
