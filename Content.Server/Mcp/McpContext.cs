#if !FULL_RELEASE || MCP
using System.Numerics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Shared.Movement.Components;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Mcp;

/// <summary>
/// Shared dependencies and helpers for MCP tools. All game state access must happen
/// on the main thread — use <see cref="RunOnMainThread{T}"/> from tool code.
/// </summary>
public sealed partial class McpContext
{
    [Dependency] public ITaskManager TaskManager = default!;
    [Dependency] public IEntityManager EntityManager = default!;
    [Dependency] public IPrototypeManager PrototypeManager = default!;
    [Dependency] public ITileDefinitionManager TileDefinitionManager = default!;
    [Dependency] public IPlayerManager PlayerManager = default!;
    [Dependency] public IConsoleHost ConsoleHost = default!;
    [Dependency] public IResourceManager ResourceManager = default!;
    [Dependency] public IConfigurationManager Configuration = default!;
    [Dependency] public ILogManager LogManager = default!;

    /// <summary>Runs a function on the game's main thread and returns its result.</summary>
    public async Task<T> RunOnMainThread<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        TaskManager.RunOnMainThread(() =>
        {
            try
            {
                tcs.TrySetResult(func());
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        });
        return await tcs.Task;
    }

    #region Argument helpers

    public static int GetInt(JsonObject args, string name)
    {
        return OptInt(args, name) ?? throw new McpToolException($"Missing required integer argument '{name}'.");
    }

    public static int? OptInt(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node == null)
            return null;
        if (node is JsonValue value && value.TryGetValue<int>(out var i))
            return i;
        throw new McpToolException($"Argument '{name}' must be an integer.");
    }

    public static double? OptDouble(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node == null)
            return null;
        if (node is JsonValue value && value.TryGetValue<double>(out var d))
            return d;
        throw new McpToolException($"Argument '{name}' must be a number.");
    }

    public static string GetString(JsonObject args, string name)
    {
        return OptString(args, name) ?? throw new McpToolException($"Missing required string argument '{name}'.");
    }

    public static string? OptString(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node == null)
            return null;
        if (node is JsonValue value && value.TryGetValue<string>(out var s))
            return s;
        throw new McpToolException($"Argument '{name}' must be a string.");
    }

    public static bool OptBool(JsonObject args, string name, bool fallback)
    {
        if (!args.TryGetPropertyValue(name, out var node) || node == null)
            return fallback;
        if (node is JsonValue value && value.TryGetValue<bool>(out var b))
            return b;
        throw new McpToolException($"Argument '{name}' must be a boolean.");
    }

    #endregion

    #region Entity id helpers

    /// <summary>Formats an entity for tool output as its NetEntity id (what console commands use).</summary>
    public int ToNetId(EntityUid uid)
    {
        return (int) EntityManager.GetNetEntity(uid).Id;
    }

    /// <summary>Resolves a NetEntity id from tool input to an EntityUid.</summary>
    public EntityUid FromNetId(int netId)
    {
        var net = new NetEntity(netId);
        if (!EntityManager.TryGetEntity(net, out var uid) || !EntityManager.EntityExists(uid))
            throw new McpToolException($"Entity {netId} does not exist.");
        return uid.Value;
    }

    #endregion

    #region Player / camera frame

    /// <summary>A player's location and screen orientation, resolved on the main thread.</summary>
    public sealed class PlayerFrame
    {
        public ICommonSession Session = default!;
        public EntityUid Entity;
        public MapId MapId;
        public EntityUid? GridUid;
        public Vector2 WorldPos;
        public Vector2i? TilePos;
        /// <summary>Eye rotation: world direction shown at the top of the screen is ScreenUp.</summary>
        public Angle EyeRotation;
        public Vector2 ScreenUp => EyeRotation.RotateVec(new Vector2(0, 1));
        public Vector2 ScreenRight => EyeRotation.RotateVec(new Vector2(1, 0));
    }

    /// <summary>
    /// Resolves a player session by name; with a null name requires exactly one connected player.
    /// Main thread only.
    /// </summary>
    public ICommonSession ResolveSession(string? playerName)
    {
        var sessions = PlayerManager.Sessions;
        if (playerName != null)
        {
            foreach (var session in sessions)
            {
                if (string.Equals(session.Name, playerName, StringComparison.OrdinalIgnoreCase))
                    return session;
            }

            throw new McpToolException($"No connected player named '{playerName}'.");
        }

        return sessions.Length switch
        {
            0 => throw new McpToolException("No players are connected."),
            1 => sessions[0],
            _ => throw new McpToolException(
                $"Multiple players connected ({string.Join(", ", Array.ConvertAll(sessions, s => s.Name))}); specify 'player'."),
        };
    }

    /// <summary>Builds the location/orientation frame for a player. Main thread only.</summary>
    public PlayerFrame GetPlayerFrame(string? playerName)
    {
        var session = ResolveSession(playerName);
        if (session.AttachedEntity is not { } entity || !EntityManager.EntityExists(entity))
            throw new McpToolException($"Player '{session.Name}' has no attached entity.");

        var transformSystem = EntityManager.System<SharedTransformSystem>();
        var mapSystem = EntityManager.System<SharedMapSystem>();
        var xform = EntityManager.GetComponent<TransformComponent>(entity);
        var worldPos = transformSystem.GetWorldPosition(entity);

        // Screen orientation, reconstructed from networked InputMoverComponent state.
        // The client's eye rotation (EyeLerpingSystem.GetRotation) is -(relative rotation + world
        // rotation of the movement-relative entity), and the view matrix bakes its negation — so the
        // world direction at the top of the screen is (0,1) rotated by +(relative + parent rotation).
        // We store that positive angle: ScreenUp/ScreenRight and screen-frame offsets RotateVec by it
        // directly. (Sign verified in-game: clockwise camera rotation moves east to the top.)
        Angle eyeRotation;
        if (EntityManager.TryGetComponent<InputMoverComponent>(entity, out var mover))
        {
            var parentRotation = mover.TargetRelativeRotation;
            if (mover.RelativeEntity is { } relative && EntityManager.EntityExists(relative))
                parentRotation += transformSystem.GetWorldRotation(relative);
            eyeRotation = parentRotation;
        }
        else
        {
            var anchor = xform.GridUid ?? xform.MapUid;
            eyeRotation = anchor is { } a ? transformSystem.GetWorldRotation(a) : Angle.Zero;
        }

        Vector2i? tilePos = null;
        if (xform.GridUid is { } gridUid && EntityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
            tilePos = mapSystem.WorldToTile(gridUid, grid, worldPos);

        return new PlayerFrame
        {
            Session = session,
            Entity = entity,
            MapId = xform.MapID,
            GridUid = xform.GridUid,
            WorldPos = worldPos,
            TilePos = tilePos,
            EyeRotation = eyeRotation,
        };
    }

    /// <summary>8-way compass name for a world vector, with north = +Y, east = +X.</summary>
    public static string Compass(Vector2 v)
    {
        if (v.LengthSquared() < 0.0001f)
            return "none";
        var deg = MathF.Atan2(v.X, v.Y) * (180f / MathF.PI);
        var index = (int) MathF.Round(deg / 45f);
        index = ((index % 8) + 8) % 8;
        return index switch
        {
            0 => "north",
            1 => "north-east",
            2 => "east",
            3 => "south-east",
            4 => "south",
            5 => "south-west",
            6 => "west",
            _ => "north-west",
        };
    }

    #endregion

    #region Coordinate resolution

    /// <summary>
    /// Resolves a target grid + tile position from tool arguments. Main thread only.
    /// Absolute form: "grid" (NetEntity id, optional if a player stands on a grid), "x", "y" (tile indices).
    /// Relative form: "relative": { "dx", "dy", "frame": "world"|"screen", "player"? } — offset from the
    /// player's tile; "screen" rotates the offset by the player's current screen orientation.
    /// </summary>
    public (EntityUid GridUid, MapGridComponent Grid, Vector2i Tile) ResolveTilePosition(JsonObject args)
    {
        PlayerFrame? frame = null;
        Vector2i tile;

        if (args.TryGetPropertyValue("relative", out var relNode) && relNode is JsonObject rel)
        {
            frame = GetPlayerFrame(OptString(rel, "player"));
            tile = ResolveRelativeTile(rel, frame);
        }
        else
        {
            var x = OptInt(args, "x");
            var y = OptInt(args, "y");
            if (x == null || y == null)
            {
                frame = GetPlayerFrame(null);
                if (frame.TilePos is not { } playerTile)
                    throw new McpToolException("Player is not standing on a grid; pass 'grid', 'x' and 'y' explicitly.");
                tile = playerTile;
            }
            else
            {
                tile = new Vector2i(x.Value, y.Value);
            }
        }

        var (gridUid, grid) = ResolveGrid(args, frame);
        return (gridUid, grid, tile);
    }

    private Vector2i ResolveRelativeTile(JsonObject rel, PlayerFrame frame)
    {
        if (frame.TilePos is not { } playerTile)
            throw new McpToolException("Player is not standing on a grid; relative addressing is unavailable.");

        var dx = OptDouble(rel, "dx") ?? 0;
        var dy = OptDouble(rel, "dy") ?? 0;
        var frameKind = OptString(rel, "frame") ?? "world";
        var offset = frameKind switch
        {
            "world" => new Vector2((float) dx, (float) dy),
            "screen" => frame.EyeRotation.RotateVec(new Vector2((float) dx, (float) dy)),
            _ => throw new McpToolException("relative.frame must be 'world' or 'screen'."),
        };

        return playerTile + new Vector2i((int) MathF.Round(offset.X), (int) MathF.Round(offset.Y));
    }

    /// <summary>
    /// Resolves the target grid: explicit "grid" argument, or the grid under the player. Main thread only.
    /// </summary>
    public (EntityUid GridUid, MapGridComponent Grid) ResolveGrid(JsonObject args, PlayerFrame? frame = null)
    {
        if (OptInt(args, "grid") is { } gridNet)
        {
            var gridUid = FromNetId(gridNet);
            if (!EntityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
                throw new McpToolException($"Entity {gridNet} is not a grid.");
            return (gridUid, grid);
        }

        frame ??= GetPlayerFrame(null);
        if (frame.GridUid is not { } playerGrid ||
            !EntityManager.TryGetComponent<MapGridComponent>(playerGrid, out var underPlayer))
        {
            throw new McpToolException("Player is not standing on a grid; pass 'grid' explicitly (see list_grids).");
        }

        return (playerGrid, underPlayer);
    }

    #endregion
}

/// <summary>Assigns single-character legend symbols to distinct keys for matrix output.</summary>
public sealed class McpLegend
{
    private const string Palette = "#=+-*%&oxabcdefghijklmnpqrstuvwyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly Dictionary<string, char> _chars = new();

    /// <summary>'.' is reserved for empty, '@' for the player, ' ' for out-of-area.</summary>
    public char Get(string key)
    {
        if (_chars.TryGetValue(key, out var existing))
            return existing;

        var c = _chars.Count < Palette.Length ? Palette[_chars.Count] : '?';
        _chars[key] = c;
        return c;
    }

    public JsonObject ToJson(string emptyName = "empty")
    {
        var json = new JsonObject { ["."] = emptyName };
        foreach (var (key, c) in _chars)
        {
            json[c.ToString()] = key;
        }

        return json;
    }
}
#endif
