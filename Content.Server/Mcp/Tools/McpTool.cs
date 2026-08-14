#if !FULL_RELEASE || MCP
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Content.Server.Mcp.Tools;

/// <summary>
/// Base class for MCP tools. Execution happens on a status-host worker thread;
/// implementations must marshal all game state access via <see cref="McpContext.RunOnMainThread{T}"/>.
/// </summary>
public abstract class McpTool(McpContext ctx)
{
    protected readonly McpContext Ctx = ctx;

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract JsonObject InputSchema { get; }

    /// <summary>Returns the tool result as JSON. Throw <see cref="McpToolException"/> for agent-facing errors.</summary>
    public abstract Task<JsonNode> ExecuteAsync(JsonObject args);
}

/// <summary>Terse JSON Schema builders for tool input schemas.</summary>
public static class Schema
{
    public static JsonObject Object(JsonObject properties, params string[] required)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
        };
        if (required.Length > 0)
        {
            var arr = new JsonArray();
            foreach (var name in required)
            {
                arr.Add(name);
            }

            schema["required"] = arr;
        }

        return schema;
    }

    public static JsonObject String(string description) =>
        new() { ["type"] = "string", ["description"] = description };

    public static JsonObject Enum(string description, params string[] values)
    {
        var arr = new JsonArray();
        foreach (var v in values)
        {
            arr.Add(v);
        }

        return new JsonObject { ["type"] = "string", ["description"] = description, ["enum"] = arr };
    }

    public static JsonObject Int(string description) =>
        new() { ["type"] = "integer", ["description"] = description };

    public static JsonObject Number(string description) =>
        new() { ["type"] = "number", ["description"] = description };

    public static JsonObject Bool(string description) =>
        new() { ["type"] = "boolean", ["description"] = description };

    public static JsonObject Array(string description, JsonObject items) =>
        new() { ["type"] = "array", ["description"] = description, ["items"] = items };

    /// <summary>The standard optional relative-addressing parameter (offset from the player's tile).</summary>
    public static JsonObject Relative(string what) => Object(new JsonObject
    {
        ["dx"] = Number($"Offset X from the player's tile for {what}."),
        ["dy"] = Number($"Offset Y from the player's tile for {what}."),
        ["frame"] = Enum("Coordinate frame: 'world' (default, +X east, +Y north) or 'screen' " +
                         "(+X right on the player's screen, +Y up; respects current screen rotation).",
            "world", "screen"),
        ["player"] = String("Player name whose position is the origin (default: the only connected player)."),
    });

    /// <summary>The standard grid parameter.</summary>
    public static JsonObject Grid() =>
        Int("Grid NetEntity id (see list_grids). Default: the grid under the player.");
}
#endif
