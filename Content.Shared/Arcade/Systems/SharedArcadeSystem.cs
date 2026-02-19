using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Systems;

public abstract partial class SharedArcadeSystem : EntitySystem
{ }

[Serializable, NetSerializable]
public sealed class ArcadeHighScoreEntry : IComparable
{
    public string Name;
    public int Score;

    public ArcadeHighScoreEntry(string name, int score)
    {
        Name = name;
        Score = score;
    }

    public int CompareTo(object? obj)
    {
        if (obj is not ArcadeHighScoreEntry entry) return 0;
        return Score.CompareTo(entry.Score);
    }
}
