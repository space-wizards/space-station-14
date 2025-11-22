using System.Linq;
using Content.Shared.Arcade.BlockGame;
using Robust.Shared.Utility;

namespace Content.Server.Arcade.Systems;

public sealed partial class ArcadeSystem
{
    private readonly List<BlockGameHighScoreEntry> _roundHighscores = new();
    private readonly List<BlockGameHighScoreEntry> _globalHighscores = new();

    public HighScorePlacement RegisterHighScore(string name, int score)
    {
        var entry = new BlockGameHighScoreEntry(name, score);
        return new HighScorePlacement(TryInsertIntoList(_roundHighscores, entry), TryInsertIntoList(_globalHighscores, entry));
    }

    public List<BlockGameHighScoreEntry> GetLocalHighscores() => GetSortedHighscores(_roundHighscores);

    public List<BlockGameHighScoreEntry> GetGlobalHighscores() => GetSortedHighscores(_globalHighscores);

    private List<BlockGameHighScoreEntry> GetSortedHighscores(List<BlockGameHighScoreEntry> highScoreEntries)
    {
        var result = highScoreEntries.ShallowClone();
        result.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));
        return result;
    }

    private int? TryInsertIntoList(List<BlockGameHighScoreEntry> highScoreEntries, BlockGameHighScoreEntry entry)
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

    private int? GetPlacement(List<BlockGameHighScoreEntry> highScoreEntries, BlockGameHighScoreEntry entry)
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
