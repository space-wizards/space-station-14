#if !FULL_RELEASE || MCP
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Server.Mcp.Tools;
using Content.Shared.CCVar;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;

namespace Content.Server.Mcp;

/// <summary>
/// Embedded MCP (Model Context Protocol) server for mapping agents.
/// Speaks stateless streamable-HTTP JSON-RPC on POST /mcp via the engine's <see cref="IStatusHost"/>.
/// Compiled only into non-FULL_RELEASE builds unless -p:EnableMcp=true is passed.
/// </summary>
public sealed partial class McpManager : IPostInjectInit
{
    public const string Endpoint = "/mcp";
    private const string ProtocolVersion = "2025-03-26";

    [Dependency] private IStatusHost _statusHost = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;
    private McpContext _ctx = default!;
    private readonly Dictionary<string, McpTool> _tools = new();

    private bool _enabled;
    private string _token = string.Empty;

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("mcp");
        _ctx = IoCManager.InjectDependencies(new McpContext());

        foreach (var tool in CreateTools(_ctx))
        {
            _tools.Add(tool.Name, tool);
        }

        _statusHost.AddHandler(HandleRequestAsync);
    }

    public void Initialize()
    {
        _config.OnValueChanged(CCVars.McpEnabled, v => _enabled = v, true);
        _config.OnValueChanged(CCVars.McpToken, v => _token = v, true);
        _sawmill.Info($"MCP mapping server available at {Endpoint} ({_tools.Count} tools), enabled: {_enabled}");
    }

    public void Shutdown()
    {
    }

    private static IEnumerable<McpTool> CreateTools(McpContext ctx)
    {
        // Player & surroundings
        yield return new PlayerStatusTool(ctx);
        yield return new LookAroundTool(ctx);
        yield return new TeleportPlayerTool(ctx);
        // Map reading
        yield return new ListMapsTool(ctx);
        yield return new ListGridsTool(ctx);
        yield return new ReadTilesTool(ctx);
        yield return new FindTilesTool(ctx);
        // Entity reading
        yield return new ReadEntitiesTool(ctx);
        yield return new FindEntitiesTool(ctx);
        yield return new EntityInfoTool(ctx);
        // Prototype catalogues
        yield return new ListEntityPrototypesTool(ctx);
        yield return new ListTilePrototypesTool(ctx);
        yield return new ListDecalPrototypesTool(ctx);
        // Map writing
        yield return new SetTilesTool(ctx);
        yield return new ReplaceTilesTool(ctx);
        yield return new SpawnEntityTool(ctx);
        yield return new DeleteEntitiesTool(ctx);
        yield return new TransformEntityTool(ctx);
        // Decals
        yield return new ReadDecalsTool(ctx);
        yield return new AddDecalTool(ctx);
        yield return new EditDecalTool(ctx);
        yield return new RemoveDecalTool(ctx);
        // Map lifecycle
        yield return new CreateMapTool(ctx);
        yield return new CreateGridTool(ctx);
        yield return new DeleteMapTool(ctx);
        yield return new PauseMapTool(ctx);
        yield return new MapInitTool(ctx);
        yield return new SaveMapTool(ctx);
        yield return new SaveGridTool(ctx);
        yield return new LoadMapTool(ctx);
        yield return new LoadGridTool(ctx);
        yield return new SetAmbientLightTool(ctx);
        yield return new MappingSessionTool(ctx);
        // Console escape hatch
        yield return new RunCommandTool(ctx);
    }

    #region HTTP

    private async Task<bool> HandleRequestAsync(IStatusHandlerContext context)
    {
        if (context.Url.AbsolutePath != Endpoint)
            return false;

        if (!_enabled)
            return false;

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Forbidden);
            return true;
        }

        if (context.RequestMethod == HttpMethod.Delete)
        {
            // Stateless server: session termination is a no-op.
            await context.RespondNoContentAsync();
            return true;
        }

        if (context.RequestMethod != HttpMethod.Post)
        {
            // No SSE stream offered.
            await context.RespondErrorAsync(HttpStatusCode.MethodNotAllowed);
            return true;
        }

        JsonNode? body;
        try
        {
            body = await JsonNode.ParseAsync(context.RequestBody);
        }
        catch (Exception)
        {
            await context.RespondJsonAsync(
                McpJsonRpc.Error(null, McpJsonRpc.ParseError, "Invalid JSON in request body"),
                HttpStatusCode.BadRequest);
            return true;
        }

        switch (body)
        {
            case JsonObject single:
            {
                var response = await DispatchAsync(single);
                if (response == null)
                    await context.RespondNoContentAsync(); // notification
                else
                    await context.RespondJsonAsync(response);
                return true;
            }
            case JsonArray batch when batch.Count > 0:
            {
                var responses = new JsonArray();
                foreach (var item in batch)
                {
                    if (item is not JsonObject obj)
                        continue;
                    if (await DispatchAsync(obj) is { } response)
                        responses.Add(response);
                }

                if (responses.Count == 0)
                    await context.RespondNoContentAsync();
                else
                    await context.RespondJsonAsync(responses);
                return true;
            }
            default:
                await context.RespondJsonAsync(
                    McpJsonRpc.Error(null, McpJsonRpc.InvalidRequest, "Expected a JSON-RPC request object"),
                    HttpStatusCode.BadRequest);
                return true;
        }
    }

    private bool CheckAccess(IStatusHandlerContext context)
    {
        if (IPAddress.IsLoopback(context.RemoteEndPoint.Address))
            return true;

        if (string.IsNullOrEmpty(_token))
            return false;

        if (!context.RequestHeaders.TryGetValue("Authorization", out var authValues))
            return false;

        var auth = authValues.ToString();
        const string prefix = "Bearer ";
        if (!auth.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(auth[prefix.Length..]),
            Encoding.UTF8.GetBytes(_token));
    }

    #endregion

    #region JSON-RPC dispatch

    /// <summary>Handles one JSON-RPC message. Returns null for notifications (no response).</summary>
    private async Task<JsonObject?> DispatchAsync(JsonObject request)
    {
        var id = request.TryGetPropertyValue("id", out var idNode) ? idNode : null;
        var isNotification = idNode == null;

        if (McpContext.OptString(request, "method") is not { } method)
            return isNotification ? null : McpJsonRpc.Error(id, McpJsonRpc.InvalidRequest, "Missing 'method'");

        var @params = request.TryGetPropertyValue("params", out var paramsNode) && paramsNode is JsonObject p
            ? p
            : new JsonObject();

        try
        {
            if (method.StartsWith("notifications/", StringComparison.Ordinal))
                return null;

            var result = method switch
            {
                "initialize" => HandleInitialize(@params),
                "ping" => new JsonObject(),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCall(@params),
                _ => null,
            };

            if (result == null)
                return isNotification ? null : McpJsonRpc.Error(id, McpJsonRpc.MethodNotFound, $"Unknown method '{method}'");

            return isNotification ? null : McpJsonRpc.Result(id, result);
        }
        catch (McpToolException e)
        {
            return isNotification ? null : McpJsonRpc.Error(id, McpJsonRpc.InvalidParams, e.Message);
        }
        catch (Exception e)
        {
            _sawmill.Error($"MCP method '{method}' failed: {e}");
            return isNotification ? null : McpJsonRpc.Error(id, McpJsonRpc.InternalError, e.Message);
        }
    }

    private static JsonNode HandleInitialize(JsonObject @params)
    {
        var requested = McpContext.OptString(@params, "protocolVersion");
        return new JsonObject
        {
            ["protocolVersion"] = requested ?? ProtocolVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject(),
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "ss14-mapper-mcp",
                ["version"] = "1.0.0",
            },
            ["instructions"] =
                "Embedded mapping server for Space Station 14. " +
                "Typical loop: player_status / look_around to see where the user is and how their screen is rotated, " +
                "then read_tiles / read_entities to inspect, then set_tiles / spawn_entity / add_decal to edit. " +
                "Most tools accept coordinates relative to the player, including in the player's screen frame. " +
                "Key invariant: maps you intend to save must stay uninitialized and paused.",
        };
    }

    private JsonNode HandleToolsList()
    {
        var tools = new JsonArray();
        foreach (var tool in _tools.Values)
        {
            tools.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["inputSchema"] = tool.InputSchema,
            });
        }

        return new JsonObject { ["tools"] = tools };
    }

    private async Task<JsonNode> HandleToolsCall(JsonObject @params)
    {
        var name = McpContext.GetString(@params, "name");
        if (!_tools.TryGetValue(name, out var tool))
            throw new McpToolException($"Unknown tool '{name}'");

        var args = @params.TryGetPropertyValue("arguments", out var argsNode) && argsNode is JsonObject a
            ? a
            : new JsonObject();

        try
        {
            var result = await tool.ExecuteAsync(args);
            return ToolResult(result.ToJsonString(), isError: false);
        }
        catch (McpToolException e)
        {
            return ToolResult($"Error: {e.Message}", isError: true);
        }
        catch (Exception e)
        {
            _sawmill.Error($"MCP tool '{name}' failed: {e}");
            return ToolResult($"Internal error in tool '{name}': {e.Message}", isError: true);
        }
    }

    private static JsonObject ToolResult(string text, bool isError)
    {
        return new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = text,
                },
            },
            ["isError"] = isError,
        };
    }

    #endregion
}
#endif
