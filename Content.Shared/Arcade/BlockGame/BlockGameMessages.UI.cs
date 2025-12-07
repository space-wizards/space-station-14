using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.BlockGame;

[Serializable, NetSerializable]
public sealed class BlockGamePlayerActionMessage : BoundUserInterfaceMessage
{
    public readonly BlockGamePlayerAction PlayerAction;
    public BlockGamePlayerActionMessage(BlockGamePlayerAction playerAction)
    {
        PlayerAction = playerAction;
    }
}

[Serializable, NetSerializable]
public sealed class BlockGameVisualUpdateMessage : BoundUserInterfaceMessage
{
    public readonly BlockGameVisualType GameVisualType;
    public readonly BlockGameBlock[] Blocks;
    public BlockGameVisualUpdateMessage(BlockGameBlock[] blocks, BlockGameVisualType gameVisualType)
    {
        Blocks = blocks;
        GameVisualType = gameVisualType;
    }
}

[Serializable, NetSerializable]
public sealed class BlockGameScoreUpdateMessage : BoundUserInterfaceMessage
{
    public readonly int Points;
    public BlockGameScoreUpdateMessage(int points)
    {
        Points = points;
    }
}

[Serializable, NetSerializable]
public sealed class BlockGameUserStatusMessage : BoundUserInterfaceMessage
{
    public readonly bool IsPlayer;

    public BlockGameUserStatusMessage(bool isPlayer)
    {
        IsPlayer = isPlayer;
    }
}

[Serializable, NetSerializable, Virtual]
public class BlockGameSetScreenMessage : BoundUserInterfaceMessage
{
    public readonly BlockGameScreen Screen;
    public readonly bool IsStarted;
    public BlockGameSetScreenMessage(BlockGameScreen screen, bool isStarted = true)
    {
        Screen = screen;
        IsStarted = isStarted;
    }
}

[Serializable, NetSerializable]
public sealed class BlockGameGameOverScreenMessage : BlockGameSetScreenMessage
{
    public readonly int FinalScore;
    public readonly int? LocalPlacement;
    public readonly int? GlobalPlacement;
    public BlockGameGameOverScreenMessage(int finalScore, int? localPlacement, int? globalPlacement) : base(BlockGameScreen.Gameover)
    {
        FinalScore = finalScore;
        LocalPlacement = localPlacement;
        GlobalPlacement = globalPlacement;
    }
}

[Serializable, NetSerializable]
public sealed class BlockGameHighScoreUpdateMessage : BoundUserInterfaceMessage
{
    public List<BlockGameHighScoreEntry> LocalHighscores;
    public List<BlockGameHighScoreEntry> GlobalHighscores;

    public BlockGameHighScoreUpdateMessage(List<BlockGameHighScoreEntry> localHighscores, List<BlockGameHighScoreEntry> globalHighscores)
    {
        LocalHighscores = localHighscores;
        GlobalHighscores = globalHighscores;
    }
}

[Serializable, NetSerializable]
public sealed class BlockGameLevelUpdateMessage : BoundUserInterfaceMessage
{
    public readonly int Level;
    public BlockGameLevelUpdateMessage(int level)
    {
        Level = level;
    }
}
