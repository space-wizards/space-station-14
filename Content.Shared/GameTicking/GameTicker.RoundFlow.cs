using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Preferences;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    /// <summary>
    /// Game's current run level.
    /// </summary>
    [ViewVariables]
    public GameRunLevel RunLevel
    {
        get;
        protected set
        {
            var old = field;
            field = value;

            var gameRunLevelChangedEvent = new GameRunLevelChangedEvent(old, value);
            RaiseLocalEvent(gameRunLevelChangedEvent);
        }
    }

    /// <summary>
    /// Ends the current round, with custom end of round text.
    /// </summary>
    /// <param name="text">Text displayed at the end of round scoreboard.</param>
    public virtual void EndRound(string text = "") { }

    /// <summary>
    /// Restarts the current round, sending the game to the lobby or starting a new round if lobby is disabled.
    /// </summary>
    public virtual void RestartRound() { }

    /// <summary>
    ///     Loads a new map, allowing systems interested in it to handle loading events.
    ///     In the base game, this is required to be used if you want to load a station.
    ///     This does not initialze maps, unles specified via the <see cref="DeserializationOptions"/>.
    /// </summary>
    /// <remarks>
    /// This is basically a wrapper around a <see cref="MapLoaderSystem"/> method that auto generate
    /// some <see cref="MapLoadOptions"/> using information in a prototype, and raise some events to allow content
    /// to modify the options and react to the map creation.
    /// </remarks>
    /// <param name="proto">Game map prototype to load in.</param>
    /// <param name="mapId">The id of the map that was loaded.</param>
    /// <param name="options">Entity loading options, including whether the maps should be initialized.</param>
    /// <param name="stationName">Name to assign to the loaded station.</param>
    /// <param name="offset">Coordinate offset for spawning grids within this map</param>
    /// <param name="rot">Rotation offset for spawning grids within this map</param>
    /// <returns>All loaded entities and grids.</returns>
    public abstract IReadOnlyList<EntityUid> LoadGameMap(
        GameMapPrototype proto,
        out MapId mapId,
        DeserializationOptions? options = null,
        string? stationName = null,
        Vector2? offset = null,
        Angle? rot = null);

    /// <summary>
    /// Gets the current round duration, returning zero if the round has not started.
    /// </summary>
    public TimeSpan GetRoundTime()
    {
        return RunLevel == GameRunLevel.PreRoundLobby ? TimeSpan.Zero : RoundDuration();
    }

    /// <summary>
    /// Gets the current number of readied up players.
    /// </summary>
    public abstract int ReadyPlayerCount();
}

public enum GameRunLevel
{
    PreRoundLobby = 0,
    InRound = 1,
    PostRound = 2
}

public sealed class GameRunLevelChangedEvent
{
    public GameRunLevel Old { get; }
    public GameRunLevel New { get; }

    public GameRunLevelChangedEvent(GameRunLevel old, GameRunLevel @new)
    {
        Old = old;
        New = @new;
    }
}

/// <summary>
///     Event raised to allow subscribers to add text to the round end summary screen.
/// </summary>
[ByRefEvent]
public record struct RoundEndTextAppendEvent
{
    private bool _doNewLine;

    public RoundEndTextAppendEvent() { }

    /// <summary>
    ///     Text to display in the round end summary screen.
    /// </summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>
    ///     Invoke this method to add text to the round end summary screen.
    /// </summary>
    /// <param name="text"></param>
    public void AddLine(string text)
    {
        if (_doNewLine)
            Text += "\n";

        Text += text;
        _doNewLine = true;
    }
}

/// <summary>
///     Event raised before readied up players are spawned and given jobs by the GameTicker.
///     You can use this to spawn people off-station, like in the case of nuke ops or wizard.
///     Remove the players you spawned from the PlayerPool and call <see cref="Shared.GameTicking.GameTicker.PlayerJoinGame"/> on them.
/// </summary>
public sealed class RulePlayerSpawningEvent
{
    /// <summary>
    ///     Pool of players to be spawned.
    ///     If you want to handle a specific player being spawned, remove it from this list and do what you need.
    /// </summary>
    /// <remarks>If you spawn a player by yourself from this event, don't forget to call <see cref="Shared.GameTicking.GameTicker.PlayerJoinGame"/> on them.</remarks>
    public List<ICommonSession> PlayerPool { get; }
    public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
    public bool Forced { get; }

    public RulePlayerSpawningEvent(List<ICommonSession> playerPool, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
    {
        PlayerPool = playerPool;
        Profiles = profiles;
        Forced = forced;
    }
}

/// <summary>
///     Event raised after players were assigned jobs by the GameTicker and have been spawned in.
///     You can give on-station people special roles by listening to this event.
/// </summary>
public sealed class RulePlayerJobsAssignedEvent
{
    public ICommonSession[] Players { get; }
    public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
    public bool Forced { get; }

    public RulePlayerJobsAssignedEvent(ICommonSession[] players, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
    {
        Players = players;
        Profiles = profiles;
        Forced = forced;
    }
}
