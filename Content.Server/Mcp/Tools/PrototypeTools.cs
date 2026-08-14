#if !FULL_RELEASE || MCP
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server.Mcp.Tools;

public sealed class ListEntityPrototypesTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "list_entity_prototypes";

    public override string Description =>
        "Searches spawnable entity prototypes by id/name/editor-suffix substring — the same catalogue the " +
        "sandbox entity spawn window shows. Returns prototype id (what spawn_entity expects), name and suffix.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["search"] = Schema.String("Case-insensitive substring of the prototype id, name or editor suffix."),
            ["limit"] = Schema.Int("Max entries (default 50)."),
        },
        "search");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var search = McpContext.GetString(args, "search");
        var limit = McpContext.OptInt(args, "limit") ?? 50;

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var matches = new JsonArray();
            var total = 0;
            foreach (var proto in Ctx.PrototypeManager.EnumeratePrototypes<EntityPrototype>()
                         .OrderBy(p => p.ID, StringComparer.OrdinalIgnoreCase))
            {
                if (proto.Abstract || proto.HideSpawnMenu)
                    continue;
                if (!proto.ID.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !proto.Name.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    (proto.EditorSuffix?.Contains(search, StringComparison.OrdinalIgnoreCase) != true))
                {
                    continue;
                }

                total++;
                if (matches.Count >= limit)
                    continue;

                matches.Add(new JsonObject
                {
                    ["id"] = proto.ID,
                    ["name"] = proto.Name,
                    ["suffix"] = proto.EditorSuffix,
                });
            }

            return new JsonObject
            {
                ["total_matches"] = total,
                ["prototypes"] = matches,
                ["truncated"] = total > matches.Count,
            };
        });
    }
}

public sealed class ListTilePrototypesTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "list_tile_prototypes";

    public override string Description =>
        "Searches tile prototypes (floors, plating, space) by id/name substring — what read_tiles legends and " +
        "set_tiles use. Empty search lists everything.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["search"] = Schema.String("Case-insensitive substring of the tile prototype id or name (default: all)."),
        ["limit"] = Schema.Int("Max entries (default 100)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var search = McpContext.OptString(args, "search") ?? "";
        var limit = McpContext.OptInt(args, "limit") ?? 100;

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var matches = new JsonArray();
            var total = 0;
            foreach (var proto in Ctx.PrototypeManager.EnumeratePrototypes<ContentTileDefinition>()
                         .OrderBy(p => p.ID, StringComparer.OrdinalIgnoreCase))
            {
                var locName = Loc.GetString(proto.Name);
                if (search.Length != 0 &&
                    !proto.ID.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !locName.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                total++;
                if (matches.Count >= limit)
                    continue;

                matches.Add(new JsonObject
                {
                    ["id"] = proto.ID,
                    ["name"] = locName,
                    ["variants"] = proto.Variants,
                });
            }

            return new JsonObject
            {
                ["total_matches"] = total,
                ["tiles"] = matches,
                ["truncated"] = total > matches.Count,
            };
        });
    }
}

public sealed class ListDecalPrototypesTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "list_decal_prototypes";

    public override string Description =>
        "Searches decal prototypes (floor markings, dirt, warning stripes...) by id or tag substring — " +
        "what add_decal expects.";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["search"] = Schema.String("Case-insensitive substring of the decal id or its tags (default: all)."),
        ["limit"] = Schema.Int("Max entries (default 100)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var search = McpContext.OptString(args, "search") ?? "";
        var limit = McpContext.OptInt(args, "limit") ?? 100;

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var matches = new JsonArray();
            var total = 0;
            foreach (var proto in Ctx.PrototypeManager.EnumeratePrototypes<DecalPrototype>()
                         .OrderBy(p => p.ID, StringComparer.OrdinalIgnoreCase))
            {
                if (proto.Abstract || !proto.ShowMenu)
                    continue;
                if (search.Length != 0 &&
                    !proto.ID.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !proto.Tags.Any(t => t.Contains(search, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                total++;
                if (matches.Count >= limit)
                    continue;

                matches.Add(new JsonObject
                {
                    ["id"] = proto.ID,
                    ["tags"] = string.Join(",", proto.Tags),
                });
            }

            return new JsonObject
            {
                ["total_matches"] = total,
                ["decals"] = matches,
                ["truncated"] = total > matches.Count,
            };
        });
    }
}
#endif
