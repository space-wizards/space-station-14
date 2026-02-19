using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Arcade.Components;
using Content.Server.Arcade.Prototypes;
using Content.Server.GameTicking.Events;
using Content.Shared.Arcade.BlockGame;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Arcade.Systems;

public sealed partial class ArcadeSystem
{
    private Dictionary<ProtoId<ArcadeScoreboardPrototype>, List<BlockGameHighScoreEntry>> _globalScoreboard = new();

    private void OnRoundStarting(ref RoundStartingEvent args)
    {
        InitializeScoreboards();
    }

    private void InitializeScoreboards()
    {
        _globalScoreboard = new();

        foreach (var scoreboard in _prototypeManager.EnumeratePrototypes<ArcadeScoreboardPrototype>())
        {
            _globalScoreboard.Add(scoreboard.ID, new());
        }
    }

    private void OnArcadeScoreboardGameEnded(Entity<ArcadeScoreboardComponent> ent, ref ArcadeGameEndedEvent args)
    {
        if (args.Player == null || args.Score == null)
            return;

        var name = MetaData(args.Player.Value).EntityName;
        var entry = new BlockGameHighScoreEntry(name, args.Score.Value);
        var localPlacement = TryInsertIntoList(ent.Comp.Scoreboard, entry);
        var globalPlacement = ent.Comp.GlobalScoreboard != null
            ? GetGlobalPlacement(ent.Comp.GlobalScoreboard.Value, entry)
            : null;

        var placement = new HighScorePlacement(globalPlacement, localPlacement);
        // TODO: Send client an update message here?
    }

    /// <summary>
    /// Gets the local top scores of an arcade machine in descending order.
    /// </summary>
    /// <remarks>
    /// Local scores are per-machine, as opposed to global scores, which are per-round.
    /// </remarks>
    /// <param name="ent">The arcade machine with a scoreboard.</param>
    /// <returns>The top local scores of this arcade machine in descending order.</returns>
    [PublicAPI]
    public static List<BlockGameHighScoreEntry> GetSortedLocalScores(Entity<ArcadeScoreboardComponent> ent)
    {
        return GetSortedHighscores(ent.Comp.Scoreboard);
    }

    /// <summary>
    /// Gets the global top scores of an arcade game in descending order.
    /// </summary>
    /// <remarks>
    /// Global scores are per-round, as opposed to local scores, which are per-machine.
    /// </remarks>
    /// <param name="ent">The arcade machine with a scoreboard.</param>
    /// <param name="highScoreEntries">The global high scores for the game, if there is a global scoreboard.</param>
    /// <returns>Whether or not the global scores were successfully fetched.</returns>
    [PublicAPI]
    public bool TryGetSortedGlobalScores(Entity<ArcadeScoreboardComponent> ent,
        [NotNullWhen(true)] out List<BlockGameHighScoreEntry>? highScoreEntries)
    {
        highScoreEntries = null;

        if (ent.Comp.GlobalScoreboard == null)
            return false;

        if (!_globalScoreboard.TryGetValue(ent.Comp.GlobalScoreboard.Value, out var scores))
            return false;

        highScoreEntries = GetSortedHighscores(scores);
        return true;
    }

    private int? GetGlobalPlacement(ProtoId<ArcadeScoreboardPrototype> scoreboard, BlockGameHighScoreEntry entry)
    {
        if (!_globalScoreboard.TryGetValue(scoreboard, out var scores))
            return null;

        return TryInsertIntoList(scores, entry);
    }

    private static List<BlockGameHighScoreEntry> GetSortedHighscores(List<BlockGameHighScoreEntry> highScoreEntries)
    {
        var result = highScoreEntries.ShallowClone();
        result.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));
        return result;
    }

    private static int? TryInsertIntoList(List<BlockGameHighScoreEntry> highScoreEntries, BlockGameHighScoreEntry entry)
    {
        // Maximum number of entries.
        // We can just add the score to the list and return its placement.
        // TODO: Un-hardcode max scoreboard count
        if (highScoreEntries.Count < 5)
        {
            highScoreEntries.Add(entry);
            return GetPlacement(highScoreEntries, entry);
        }

        // Otherwise: If the score is lower than the top 5, it does not have a placement.
        if (highScoreEntries.Min(e => e.Score) >= entry.Score) return null;

        // If this is a new top-5 score, then we add it to the list, then remove the new lowest score from the list.
        var lowestHighscore = highScoreEntries.Min();
        if (lowestHighscore == null)
            return null;

        highScoreEntries.Remove(lowestHighscore);
        highScoreEntries.Add(entry);
        return GetPlacement(highScoreEntries, entry);
    }

    private static int? GetPlacement(List<BlockGameHighScoreEntry> highScoreEntries, BlockGameHighScoreEntry entry)
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
