using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Systems;

public abstract partial class SharedArcadeSystem : EntitySystem
{ }

/// <summary>
/// Represents a single entry on the scoreboard of an arcade game.
/// </summary>
[Serializable, NetSerializable]
public sealed class ArcadeHighScoreEntry : IComparable
{
    /// <summary>
    /// The name of the player associated with this high score entry.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string Name;

    /// <summary>
    /// The score associated with this high score entry.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
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
