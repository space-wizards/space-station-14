using Robust.Shared.Serialization;

namespace Content.Shared.Arcade;
public abstract partial class MineGameShared : Component
{
    [Serializable, NetSerializable]
    public enum MineGameArcadeUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class MineGameRequestDataMessage : BoundUserInterfaceMessage { }

    [Serializable, NetSerializable]
    public struct MineGameBoardSettings(Vector2i boardSize, int mineCount)
    {
        public readonly Vector2i BoardSize = boardSize;
        public readonly int MineCount = mineCount;
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
    public enum MineGameTileVisState
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

    /// <summary>
    /// Stores information on game start or completion time, whether game is active/running, and unflagged mine count.
    /// </summary>
    [Serializable, NetSerializable]

    public sealed class MineGameMetadata(TimeSpan referenceTime, bool running, int remainingMines) : BoundUserInterfaceMessage
    {
        public readonly TimeSpan ReferenceTime = referenceTime;
        public readonly bool Running = running;
        public readonly int RemainingMines = remainingMines;
    }

    /// <summary>
    /// Message with all information for the visual state of an ongoing, completed, or not started game.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MineGameBoardUpdateMessage(int boardWidth, MineGameTileVisState[] tileStates, MineGameMetadata? metadata) : BoundUserInterfaceMessage
    {
        public readonly int BoardWidth = boardWidth;
        public readonly MineGameTileVisState[] TileStates = tileStates;
        public readonly MineGameMetadata? Metadata = metadata;
    }
}
