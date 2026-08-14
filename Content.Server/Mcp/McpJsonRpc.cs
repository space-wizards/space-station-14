#if !FULL_RELEASE || MCP
using System.Text.Json.Nodes;

namespace Content.Server.Mcp;

/// <summary>
/// Minimal JSON-RPC 2.0 primitives for the embedded MCP server.
/// </summary>
public static class McpJsonRpc
{
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;

    public static JsonObject Result(JsonNode? id, JsonNode result)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["result"] = result,
        };
    }

    public static JsonObject Error(JsonNode? id, int code, string message)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
            },
        };
    }
}

/// <summary>
/// Thrown by MCP tools to report a user-facing (agent-facing) error.
/// Becomes a tool result with isError=true rather than a protocol error.
/// </summary>
public sealed class McpToolException(string message) : Exception(message);
#endif
