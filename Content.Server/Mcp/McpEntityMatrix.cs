#if !FULL_RELEASE || MCP
using System.Linq;
using System.Text;
using Content.Server.Spawners.Components;
using Content.Shared.Doors.Components;
using Content.Shared.SubFloor;
using Content.Shared.Tag;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Mcp;

/// <summary>
/// Shared logic for anchored-entity matrices. A matrix cell can only show one entity, so the
/// representative is picked by a fixed semantic tier — the choice depends only on the tile's own
/// contents, never on chunk order or on what else is inside the requested rectangle. Everything
/// the cell does not show must be returned to the agent through the 'stacked' dictionary and the
/// 'subfloor' layer, so no entity is ever silently lost.
/// </summary>
public static class McpEntityMatrix
{
    private static readonly ProtoId<TagPrototype> CatwalkTag = "Catwalk";

    /// <summary>A tile's anchored entities: the main layer (tier-sorted, best first) and subfloor.</summary>
    public readonly record struct Cell(List<string>? Main, List<string>? Sub)
    {
        public bool IsEmpty => Main == null && Sub == null;
    }

    /// <summary>Higher tier wins the cell; ties break by prototype id for determinism.</summary>
    public static int Tier(IEntityManager entMan, EntityUid uid)
    {
        // Logic markers: invisible or unique things the agent must never miss.
        if (entMan.HasComponent<SpawnPointComponent>(uid))
            return 4;

        if (entMan.HasComponent<DoorComponent>(uid))
            return 3;

        // Bulk walkable background loses to everything else.
        if (entMan.System<TagSystem>().HasTag(uid, CatwalkTag))
            return 1;

        return 2;
    }

    /// <summary>Collects and orders a tile's anchored entities. The filter may be null.</summary>
    public static Cell Collect(
        IEntityManager entMan,
        Robust.Shared.GameObjects.SharedMapSystem mapSystem,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile,
        Func<EntityUid, MetaDataComponent, TransformComponent, bool>? filter = null)
    {
        List<(string Proto, int Tier)>? main = null;
        List<string>? sub = null;

        var anchored = mapSystem.GetAnchoredEntities(gridUid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            var meta = entMan.GetComponent<MetaDataComponent>(uid.Value);
            if (meta.EntityPrototype?.ID is not { } proto)
                continue;

            if (filter != null && !filter(uid.Value, meta, entMan.GetComponent<TransformComponent>(uid.Value)))
                continue;

            if (entMan.HasComponent<SubFloorHideComponent>(uid.Value))
                (sub ??= new List<string>()).Add(proto);
            else
                (main ??= new List<(string, int)>()).Add((proto, Tier(entMan, uid.Value)));
        }

        main?.Sort(static (a, b) => a.Tier != b.Tier ? b.Tier - a.Tier : string.CompareOrdinal(a.Proto, b.Proto));
        sub?.Sort(StringComparer.Ordinal);
        return new Cell(main?.Select(static t => t.Proto).ToList(), sub);
    }

    /// <summary>Parses the optional 'layers' argument: which matrix layers the caller wants.</summary>
    public static (bool Main, bool Sub) ParseLayers(System.Text.Json.Nodes.JsonObject args)
    {
        if (args["layers"] is not System.Text.Json.Nodes.JsonArray layers || layers.Count == 0)
            return (true, true);

        var main = false;
        var sub = false;
        foreach (var node in layers)
        {
            switch (node?.GetValue<string>())
            {
                case "main":
                    main = true;
                    break;
                case "subfloor":
                    sub = true;
                    break;
                case var other:
                    throw new McpToolException($"Unknown layer '{other}'; expected 'main' or 'subfloor'.");
            }
        }

        return (main, sub);
    }

    /// <summary>
    /// Records everything the matrices do not show for this tile — the no-loss guarantee lives here.
    /// </summary>
    public static void AddStacked(
        System.Text.Json.Nodes.JsonObject stacked,
        Vector2i tile,
        Cell cell,
        bool wantMain,
        bool wantSub)
    {
        List<string>? hidden = null;
        if (wantMain && cell.Main is { Count: > 1 })
            (hidden ??= new List<string>()).AddRange(cell.Main.Skip(1));
        if (wantSub && cell.Sub is { Count: > 1 })
            (hidden ??= new List<string>()).AddRange(cell.Sub.Skip(1));

        if (hidden != null)
        {
            stacked[$"{tile.X},{tile.Y}"] = new System.Text.Json.Nodes.JsonArray(
                hidden.Select(static p => (System.Text.Json.Nodes.JsonNode) p).ToArray());
        }
    }

    /// <summary>
    /// Appends one cell. Compact: one char, glued. Wide: two chars ("symbol" + hidden count digit
    /// or '.'), space-separated, so cells stay countable and BPE tokenization cannot merge them.
    /// </summary>
    public static void AppendCell(StringBuilder row, char symbol, int hiddenCount, bool wide)
    {
        if (!wide)
        {
            row.Append(symbol);
            return;
        }

        if (row.Length > 0)
            row.Append(' ');
        row.Append(symbol);
        row.Append(hiddenCount > 0 ? (char) ('0' + Math.Min(hiddenCount, 9)) : '.');
    }
}
#endif
