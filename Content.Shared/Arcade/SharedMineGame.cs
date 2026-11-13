using Robust.Shared.Serialization;

namespace Content.Shared.Arcade;

[Serializable, NetSerializable]
public enum MineGameArcadeUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class MineGameRequestDataMessage : BoundUserInterfaceMessage { }

[DataDefinition, Serializable, NetSerializable]
public partial struct MineGameBoardSettings
{
    [DataField("boardSize")]
    public Vector2i BoardSize;
    [DataField("mineCount")]
    public int MineCount;
}

[Serializable, NetSerializable]
public sealed class MineGameRequestNewBoardMessage(MineGameBoardSettings settings) : BoundUserInterfaceMessage
{
    public readonly MineGameBoardSettings Settings = settings;
}

[Serializable, NetSerializable]
public enum MineGameTileActionType
{
    Clear,
    Flag,
    Unflag
}

[Serializable, NetSerializable]
public struct MineGameTileAction(Vector2i tileLocation, MineGameTileActionType actionType)
{
    public readonly Vector2i TileLocation = tileLocation;
    public readonly MineGameTileActionType ActionType = actionType;
}

[Serializable, NetSerializable]
public sealed class MineGameTileActionMessage(MineGameTileAction tileAction) : BoundUserInterfaceMessage
{
    public readonly MineGameTileAction TileAction = tileAction;
}

[Serializable, NetSerializable]
public enum MineGameTileVisState : byte
{ // all this to avoid sending true game state (such as actual random mine locs) to client
    Uncleared,
    Flagged,
    Mine,
    FalseFlagged,
    MineDetonated,

    ClearedEmpty,
    ClearedOne,
    ClearedTwo,
    ClearedThree,
    ClearedFour,
    ClearedFive,
    ClearedSix,
    ClearedSeven,
    ClearedEight
}

/// <summary>
/// The current status of the game. Can be uninitialized, running (initialized, initialized with mines), or finished (win/loss).
/// </summary>
[Serializable, NetSerializable]
public enum MineGameStatus : byte
{
    /// <summary>
    /// Game is uninitialized with potentially invalid board data. This is a temporary, bad state.
    /// </summary>
    /// <remarks>
    /// This should only be the case when a map or player creates a new arcade entity and there is no
    /// preloaded game data. The ArcadeSystem should then initialize the game and leave this state.
    /// </remarks>
    Uninitialized,

    /// <summary>
    /// Game has been initialized (has a valid board configuration), but mines have not been generated; no
    /// initial clear has been performed
    /// </summary>
    Initialized,

    /// <summary>
    /// Game has already been initialized (Initialized state), and had mines spawned with initial clear.
    /// This is the primary 'active'/'running' game state.
    /// </summary>
    /// <remarks>
    /// It is necessary to generate mines only after the initial clear because of the option for a safe-start
    /// tile radius, preventing mines from spawning on the first clear where the player has zero mine information.
    /// </remarks>
    MinesSpawned,

    /// <summary>
    /// A game has been initialized, and the player(s) won and have not started a new game.
    /// </summary>
    Won,
    /// <summary>
    /// A game has been initialized, and the player(s) lost and have not started a new game.
    /// </summary>
    Lost,
}
