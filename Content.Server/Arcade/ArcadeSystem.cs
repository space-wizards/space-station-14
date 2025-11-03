using System.Linq;
using Content.Server.UserInterface;
using Content.Shared.Arcade;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Arcade;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed partial class ArcadeSystem : SharedArcadeSystem
{
    private readonly List<BlockGameMessages.HighScoreEntry> _roundHighscores = new();
    private readonly List<BlockGameMessages.HighScoreEntry> _globalHighscores = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    public HighScorePlacement RegisterHighScore(string name, int score)
    {
        var entry = new BlockGameMessages.HighScoreEntry(name, score);
        return new HighScorePlacement(TryInsertIntoList(_roundHighscores, entry), TryInsertIntoList(_globalHighscores, entry));
    }

    public List<BlockGameMessages.HighScoreEntry> GetLocalHighscores() => GetSortedHighscores(_roundHighscores);

    public List<BlockGameMessages.HighScoreEntry> GetGlobalHighscores() => GetSortedHighscores(_globalHighscores);

    private List<BlockGameMessages.HighScoreEntry> GetSortedHighscores(List<BlockGameMessages.HighScoreEntry> highScoreEntries)
    {
        var result = highScoreEntries.ShallowClone();
        result.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));
        return result;
    }

    private int? TryInsertIntoList(List<BlockGameMessages.HighScoreEntry> highScoreEntries, BlockGameMessages.HighScoreEntry entry)
    {
        if (highScoreEntries.Count < 5)
        {
            highScoreEntries.Add(entry);
            return GetPlacement(highScoreEntries, entry);
        }

        if (highScoreEntries.Min(e => e.Score) >= entry.Score) return null;

        var lowestHighscore = highScoreEntries.Min();

        if (lowestHighscore == null) return null;

        highScoreEntries.Remove(lowestHighscore);
        highScoreEntries.Add(entry);
        return GetPlacement(highScoreEntries, entry);

    }

    private int? GetPlacement(List<BlockGameMessages.HighScoreEntry> highScoreEntries, BlockGameMessages.HighScoreEntry entry)
    {
        int? placement = null;
        if (highScoreEntries.Contains(entry))
        {
            highScoreEntries.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));
            placement = 1 + highScoreEntries.IndexOf(entry);
        }

        return placement;
    }

    public readonly struct HighScorePlacement
    {
        public readonly int? GlobalPlacement;
        public readonly int? LocalPlacement;

        public HighScorePlacement(int? globalPlacement, int? localPlacement)
        {
            GlobalPlacement = globalPlacement;
            LocalPlacement = localPlacement;
        }
    }
}

/// <summary>
///     Called on the arcade machine entity when a game ends for any reason.
/// </summary>
/// <param name="player">The entity playing the arcade game.</param>
/// <param name="result">The result of the game.</param>
public sealed class ArcadeGameEndedEvent(EntityUid? player,
    ArcadeGameResult result = ArcadeGameResult.Forfeit)
    : EntityEventArgs
{
    public EntityUid? Player = player;
    public ArcadeGameResult Result = result;
}

/// <summary>
///     Called on the arcade game player entity when they finish an arcade game for any reason.
/// </summary>
/// <param name="result">The result of the game.</param>
public sealed class FinishedArcadeGameEvent(ArcadeGameResult result) : EntityEventArgs
{
    public ArcadeGameResult Result = result;
}

public enum ArcadeGameResult
{
    /// <summary>
    /// Player has won the game.
    /// </summary>
    Win,

    /// <summary>
    /// Game ends, and the player neither won nor lost.
    /// </summary>
    Draw,

    /// <summary>
    /// The player forfeits the game.
    /// </summary>
    Forfeit,

    /// <summary>
    /// The player lost the game.
    /// </summary>
    Fail,
}
