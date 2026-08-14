#if !FULL_RELEASE || MCP
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Mcp.Tools;

/// <summary>Shared entity filter parsing for read/find/delete tools.</summary>
public sealed class EntityFilter
{
    public string? Prototype;
    public string? NameContains;
    public Type? Component;
    public bool AnchoredOnly;

    public static JsonObject SchemaProperties() => new()
    {
        ["prototype"] = Schema.String("Only entities with this exact prototype id."),
        ["name_contains"] = Schema.String("Only entities whose in-game name (localized, often Russian) OR prototype id contains this (case-insensitive)."),
        ["component"] = Schema.String("Only entities with this component (e.g. 'Door', 'ApcPowerReceiver')."),
        ["anchored_only"] = Schema.Bool("Only anchored (bolted-down) entities."),
    };

    public static EntityFilter Parse(McpContext ctx, JsonObject args)
    {
        var filter = new EntityFilter
        {
            Prototype = McpContext.OptString(args, "prototype"),
            NameContains = McpContext.OptString(args, "name_contains"),
            AnchoredOnly = McpContext.OptBool(args, "anchored_only", false),
        };

        if (McpContext.OptString(args, "component") is { } compName)
        {
            if (!ctx.EntityManager.ComponentFactory.TryGetRegistration(compName, out var registration))
                throw new McpToolException($"Unknown component '{compName}'.");
            filter.Component = registration.Type;
        }

        return filter;
    }

    public bool Matches(McpContext ctx, EntityUid uid, MetaDataComponent meta, TransformComponent xform)
    {
        if (AnchoredOnly && !xform.Anchored)
            return false;
        if (Prototype != null && meta.EntityPrototype?.ID != Prototype)
            return false;
        // Entity names are localized; match the prototype id too so English searches keep working.
        if (NameContains != null &&
            !meta.EntityName.Contains(NameContains, StringComparison.OrdinalIgnoreCase) &&
            !(meta.EntityPrototype?.ID.Contains(NameContains, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return false;
        }
        if (Component != null && !ctx.EntityManager.HasComponent(uid, Component))
            return false;
        return true;
    }
}

public sealed class ReadEntitiesTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;

    public override string Name => "read_entities";

    public override string Description =>
        "Lists entities inside a rectangle of a grid (or draws the anchored ones as a matrix). " +
        "Filter by prototype, name, component or anchored state. In absolute form x,y is the south-west corner; " +
        "with 'relative' the player-offset tile is the center. CAUTION: a matrix cell shows only the tile's " +
        "highest-priority anchored entity (transitions/markers > doors > other > catwalks) — everything it " +
        "hides is returned in 'stacked' (\"x,y\" -> prototypes) and cables/pipes in the separate 'subfloor' " +
        "matrix; consult both for the full picture, or use list mode for lossless output.";

    public override JsonObject InputSchema
    {
        get
        {
            var props = new JsonObject
            {
                ["grid"] = Schema.Grid(),
                ["x"] = Schema.Int("South-west corner tile X (absolute form)."),
                ["y"] = Schema.Int("South-west corner tile Y (absolute form)."),
                ["relative"] = Schema.Relative("the center of the rectangle"),
                ["width"] = Schema.Int("Rectangle width in tiles (default 21)."),
                ["height"] = Schema.Int("Rectangle height in tiles (default 21)."),
                ["mode"] = Schema.Enum("'list' (default) or 'matrix' (anchored entities as characters).", "list", "matrix"),
                ["format"] = Schema.Enum(
                    "Matrix cell format: 'compact' (default, one char per tile) or 'wide' (two chars per cell, " +
                    "space-separated: entity symbol + digit count of hidden entities, '.' if none).",
                    "compact", "wide"),
                ["layers"] = Schema.Array(
                    "Matrix layers to return: 'main' (walls, doors, furniture...) and/or 'subfloor' " +
                    "(cables, pipes). Default: both.",
                    Schema.Enum("Layer name.", "main", "subfloor")),
                ["limit"] = Schema.Int("Max list entries (default 200)."),
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
        var width = McpContext.OptInt(args, "width") ?? 21;
        var height = McpContext.OptInt(args, "height") ?? 21;
        var limit = McpContext.OptInt(args, "limit") ?? 200;
        var mode = McpContext.OptString(args, "mode") ?? "list";
        if (width < 1 || height < 1 || (long) width * height > MaxArea)
            throw new McpToolException($"width/height must be positive with area <= {MaxArea}.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var filter = EntityFilter.Parse(Ctx, args);
            var (gridUid, grid, anchor) = Ctx.ResolveTilePosition(args);
            var corner = args.ContainsKey("relative")
                ? anchor - new Vector2i(width / 2, height / 2)
                : anchor;

            var result = new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["south_west_corner"] = new JsonObject { ["x"] = corner.X, ["y"] = corner.Y },
            };

            if (mode == "matrix")
            {
                var wide = (McpContext.OptString(args, "format") ?? "compact") == "wide";
                var (wantMain, wantSub) = McpEntityMatrix.ParseLayers(args);
                var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
                var legend = new McpLegend();
                var subLegend = new McpLegend();
                var rows = new JsonArray();
                var subRows = new JsonArray();
                var stacked = new JsonObject();
                var anySub = false;
                for (var y = corner.Y + height - 1; y >= corner.Y; y--)
                {
                    var row = new StringBuilder(width);
                    var subRow = new StringBuilder(width);
                    for (var x = corner.X; x < corner.X + width; x++)
                    {
                        var cell = McpEntityMatrix.Collect(Ctx.EntityManager, mapSystem, gridUid, grid,
                            new Vector2i(x, y), (uid, meta, xform) => filter.Matches(Ctx, uid, meta, xform));

                        var mainHidden = wantMain && cell.Main is { Count: > 1 } ? cell.Main.Count - 1 : 0;
                        var subHidden = wantSub && cell.Sub is { Count: > 1 } ? cell.Sub.Count - 1 : 0;
                        McpEntityMatrix.AddStacked(stacked, new Vector2i(x, y), cell, wantMain, wantSub);

                        if (wantMain)
                        {
                            McpEntityMatrix.AppendCell(row,
                                cell.Main is { Count: > 0 } ? legend.Get(cell.Main[0]) : '.', mainHidden, wide);
                        }

                        if (wantSub)
                        {
                            anySub |= cell.Sub is { Count: > 0 };
                            McpEntityMatrix.AppendCell(subRow,
                                cell.Sub is { Count: > 0 } ? subLegend.Get(cell.Sub[0]) : '.', subHidden, wide);
                        }
                    }

                    if (wantMain)
                        rows.Add(row.ToString());
                    if (wantSub)
                        subRows.Add(subRow.ToString());
                }

                result["orientation"] = "world-aligned: top row = north, left column = west";
                if (wantMain)
                {
                    result["legend"] = legend.ToJson("no matching anchored entity");
                    result["rows"] = rows;
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
                return result;
            }

            var lookup = Ctx.EntityManager.System<EntityLookupSystem>();
            var found = new HashSet<EntityUid>();
            lookup.GetLocalEntitiesIntersecting(gridUid,
                new Box2(corner.X, corner.Y, corner.X + width, corner.Y + height), found);

            var entities = new JsonArray();
            var truncated = false;
            foreach (var uid in found)
            {
                var meta = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid);
                var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);
                if (!filter.Matches(Ctx, uid, meta, xform))
                    continue;
                if (entities.Count >= limit)
                {
                    truncated = true;
                    break;
                }

                entities.Add(EntityTools.Describe(Ctx, uid, meta, xform));
            }

            result["entities"] = entities;
            result["truncated"] = truncated;
            return result;
        });
    }
}

public sealed class FindEntitiesTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "find_entities";

    public override string Description =>
        "Searches ALL entities (optionally limited to one map or grid) by prototype id, name substring and/or " +
        "component. name_contains matches both the in-game name (localized, often Russian) and the prototype id, " +
        "so English terms like 'ladder' work. Use to find spawn markers, doors, APCs etc. anywhere on the map.";

    public override JsonObject InputSchema
    {
        get
        {
            var props = new JsonObject
            {
                ["map"] = Schema.Int("Restrict to this map id."),
                ["grid"] = Schema.Int("Restrict to this grid (NetEntity id)."),
                ["limit"] = Schema.Int("Max entries (default 100)."),
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
        var limit = McpContext.OptInt(args, "limit") ?? 100;
        var mapFilter = McpContext.OptInt(args, "map");
        var gridFilter = McpContext.OptInt(args, "grid");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var filter = EntityFilter.Parse(Ctx, args);
            if (filter.Prototype == null && filter.NameContains == null && filter.Component == null)
                throw new McpToolException("Provide at least one of: prototype, name_contains, component.");

            EntityUid? gridUid = gridFilter is { } g ? Ctx.FromNetId(g) : null;
            var mapId = mapFilter is { } m ? new MapId(m) : (MapId?) null;

            var entities = new JsonArray();
            var truncated = false;
            var total = 0;

            var query = Ctx.EntityManager.AllEntityQueryEnumerator<MetaDataComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var meta, out var xform))
            {
                if (mapId != null && xform.MapID != mapId)
                    continue;
                if (gridUid != null && xform.GridUid != gridUid)
                    continue;
                if (!filter.Matches(Ctx, uid, meta, xform))
                    continue;

                total++;
                if (entities.Count >= limit)
                {
                    truncated = true;
                    continue;
                }

                entities.Add(EntityTools.Describe(Ctx, uid, meta, xform));
            }

            return new JsonObject
            {
                ["total_matches"] = total,
                ["entities"] = entities,
                ["truncated"] = truncated,
            };
        });
    }
}

public sealed class EntityInfoTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "entity_info";

    public override string Description =>
        "Details of one entity: prototype, name, position, rotation, anchoring, parent, children count and its " +
        "component list. Pass 'component' to dump that component's simple fields (a la view-variables).";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["entity"] = Schema.Int("Entity NetEntity id."),
            ["component"] = Schema.String("Component name whose fields to dump (optional)."),
        },
        "entity");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var netId = McpContext.GetInt(args, "entity");
        var componentName = McpContext.OptString(args, "component");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var uid = Ctx.FromNetId(netId);
            var meta = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid);
            var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);

            var result = EntityTools.Describe(Ctx, uid, meta, xform);
            result["description"] = meta.EntityDescription;
            result["parent"] = xform.ParentUid.IsValid() ? Ctx.ToNetId(xform.ParentUid) : null;
            result["child_count"] = xform.ChildCount;
            result["paused"] = meta.EntityPaused;

            var components = new JsonArray();
            foreach (var component in Ctx.EntityManager.GetComponents(uid))
            {
                components.Add(Ctx.EntityManager.ComponentFactory.GetComponentName(component.GetType()));
            }

            result["components"] = components;

            if (componentName != null)
            {
                if (!Ctx.EntityManager.ComponentFactory.TryGetRegistration(componentName, out var registration) ||
                    !Ctx.EntityManager.TryGetComponent(uid, registration.Type, out var component))
                {
                    throw new McpToolException($"Entity has no component '{componentName}'.");
                }

                result["component_fields"] = DumpComponent(component);
            }

            return (JsonNode) result;
        });
    }

    private JsonObject DumpComponent(IComponent component)
    {
        var dump = new JsonObject();
        var type = component.GetType();
        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
        {
            object? value;
            try
            {
                switch (member)
                {
                    case FieldInfo field:
                        value = field.GetValue(component);
                        break;
                    case PropertyInfo { CanRead: true, GetMethod.IsPublic: true } property
                        when property.GetIndexParameters().Length == 0:
                        value = property.GetValue(component);
                        break;
                    default:
                        continue;
                }
            }
            catch (Exception)
            {
                continue;
            }

            if (dump.Count >= 120)
                break;

            dump[member.Name] = value switch
            {
                null => null,
                bool or byte or sbyte or short or ushort or int or uint or long or float or double or string
                    => JsonValue.Create(value.ToString()),
                Enum or Angle or Color or EntityUid or NetEntity or Vector2i or EntProtoId
                    => value.ToString(),
                System.Numerics.Vector2 v => $"({v.X:0.##}, {v.Y:0.##})",
                System.Collections.ICollection collection => $"[{collection.Count} items]",
                _ => null,
            };
        }

        return dump;
    }
}

/// <summary>Standard one-line entity description shared by tools.</summary>
public static class EntityTools
{
    public static JsonObject Describe(McpContext ctx, EntityUid uid, MetaDataComponent meta, TransformComponent xform)
    {
        var transformSystem = ctx.EntityManager.System<SharedTransformSystem>();
        var entry = new JsonObject
        {
            ["entity"] = ctx.ToNetId(uid),
            ["prototype"] = meta.EntityPrototype?.ID,
            ["name"] = meta.EntityName,
            ["anchored"] = xform.Anchored,
        };

        if (xform.GridUid is { } gridUid &&
            ctx.EntityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
        {
            var mapSystem = ctx.EntityManager.System<SharedMapSystem>();
            var tile = mapSystem.WorldToTile(gridUid, grid, transformSystem.GetWorldPosition(uid));
            entry["tile_x"] = tile.X;
            entry["tile_y"] = tile.Y;
        }
        else
        {
            var world = transformSystem.GetWorldPosition(uid);
            entry["world_x"] = MathF.Round(world.X, 2);
            entry["world_y"] = MathF.Round(world.Y, 2);
        }

        var rotation = xform.LocalRotation;
        entry["rotation_deg"] = Math.Round(rotation.Degrees, 1);
        entry["facing"] = rotation.GetDir().ToString();
        return entry;
    }
}
#endif
