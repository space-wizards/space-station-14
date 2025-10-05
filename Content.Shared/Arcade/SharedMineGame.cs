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
    None, // Small quirk of mine game netcode; client should keep stored visual state

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
