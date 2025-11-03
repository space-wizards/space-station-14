using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.BlockGame;

public enum BlockGameVisualType
{
    GameField,
    HoldBlock,
    NextBlock
}

[Serializable, NetSerializable]
public enum BlockGameScreen
{
    Game,
    Pause,
    Gameover,
    Highscores
}

[Serializable, NetSerializable]
public sealed class BlockGameHighScoreEntry : IComparable
{
    public string Name;
    public int Score;

    public BlockGameHighScoreEntry(string name, int score)
    {
        Name = name;
        Score = score;
    }

    public int CompareTo(object? obj)
    {
        if (obj is not BlockGameHighScoreEntry entry) return 0;
        return Score.CompareTo(entry.Score);
    }
}
