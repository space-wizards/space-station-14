using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Arcade.Components;
using Content.Server.Arcade.Prototypes;
using Content.Shared.Arcade.Systems;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Arcade.Systems;

public sealed partial class ArcadeSystem
{
    // TODO: Implement persistence for the global scoreboard. This needs database work.
    private Dictionary<ProtoId<ArcadeScoreboardPrototype>, List<ArcadeHighScoreEntry>> _globalScoreboard = new();
    private Dictionary<ProtoId<ArcadeScoreboardPrototype>, List<ArcadeHighScoreEntry>> _localScoreboard = new();

    private void InitializeScoreboards()
    {
        _globalScoreboard = new();
        _localScoreboard = new();
        FillMissingScoreboards();
    }

    private void FillMissingScoreboards()
    {
        foreach (var scoreboard in _prototypeManager.EnumeratePrototypes<ArcadeScoreboardPrototype>())
        {
            if (!_globalScoreboard.ContainsKey(scoreboard.ID))
                _globalScoreboard.Add(scoreboard.ID, new());

            if (!_localScoreboard.ContainsKey(scoreboard.ID))
                _localScoreboard.Add(scoreboard.ID, new());
        }
    }

    private void OnArcadeScoreboardGameEnded(Entity<ArcadeScoreboardComponent> ent, ref ArcadeGameEndedEvent args)
    {
        if (args.Player == null || args.Score == null)
            return;

        var name = MetaData(args.Player.Value).EntityName;
        var entry = new ArcadeHighScoreEntry(name, args.Score.Value);

        SubmitScore(ent, entry, args.Player.Value);
    }

    private void SubmitScore(Entity<ArcadeScoreboardComponent> ent, ArcadeHighScoreEntry entry, EntityUid player)
    {
        var serverScoreboard = ent.Comp.ServerScoreboard;

        var globalPlacement = GetServerPlacement(_globalScoreboard, serverScoreboard, entry);
        var localPlacement = GetServerPlacement(_localScoreboard, serverScoreboard, entry);
        var machinePlacement = TryInsertIntoList(ent.Comp.Scoreboard, entry, GetMaxEntries(ent));

        var placement = new HighScorePlacement(globalPlacement, localPlacement, machinePlacement);
        var placementEvent = new ArcadeScorePlacementSubmittedEvent(player, entry.Score, placement);

        RaiseLocalEvent(ent.Owner, ref placementEvent);
    }

    /// <summary>
    /// Gets the machine top scores of a specific arcade machine in descending order.
    /// </summary>
    /// <remarks>
    /// Global scores are all-time scores. Local scores are per-session scores. Machine scores are per-entity scores.
    /// </remarks>
    /// <param name="ent">The arcade machine with a scoreboard.</param>
    /// <returns>The top machine scores of this arcade machine in descending order.</returns>
    [PublicAPI]
    public List<ArcadeHighScoreEntry> GetSortedMachineScores(Entity<ArcadeScoreboardComponent> ent,
        out int maxScores)
    {
        maxScores = GetMaxEntries(ent);
        return GetSortedHighscores(ent.Comp.Scoreboard);
    }

    /// <summary>
    /// Gets the global top scores of an arcade game in descending order.
    /// </summary>
    /// <remarks>
    /// Global scores are all-time scores. Local scores are per-session scores. Machine scores are per-entity scores.
    /// </remarks>
    /// <param name="ent">The arcade machine with a scoreboard.</param>
    /// <param name="highScoreEntries">The global high scores for the game, if there is a global scoreboard.</param>
    /// <returns>Whether or not the global scores were successfully fetched.</returns>
    [PublicAPI]
    public bool TryGetSortedGlobalScores(Entity<ArcadeScoreboardComponent> ent,
        [NotNullWhen(true)] out List<ArcadeHighScoreEntry>? highScoreEntries,
        [NotNullWhen(true)] out int? maxScores)
    {
        return TryGetSortedServerScores(_globalScoreboard, ent, out highScoreEntries, out maxScores);
    }

    /// <summary>
    /// Gets the local top scores of an arcade game in descending order.
    /// </summary>
    /// <remarks>
    /// Global scores are all-time scores. Local scores are per-session scores. Machine scores are per-entity scores.
    /// </remarks>
    /// <param name="ent">The arcade machine with a scoreboard.</param>
    /// <param name="highScoreEntries">The local high scores for the game, if there is a local scoreboard.</param>
    /// <returns>Whether or not the local scores were successfully fetched.</returns>
    [PublicAPI]
    public bool TryGetSortedLocalScores(Entity<ArcadeScoreboardComponent> ent,
        [NotNullWhen(true)] out List<ArcadeHighScoreEntry>? highScoreEntries,
        [NotNullWhen(true)] out int? maxScores)
    {
        return TryGetSortedServerScores(_localScoreboard, ent, out highScoreEntries, out maxScores);
    }

    private bool TryGetSortedServerScores(
        Dictionary<ProtoId<ArcadeScoreboardPrototype>, List<ArcadeHighScoreEntry>> serverScores,
        Entity<ArcadeScoreboardComponent> ent,
        [NotNullWhen(true)] out List<ArcadeHighScoreEntry>? highScoreEntries,
        [NotNullWhen(true)] out int? maxScores)
    {
        highScoreEntries = null;
        maxScores = null;

        if (ent.Comp.ServerScoreboard == null)
            return false;

        if (!serverScores.TryGetValue(ent.Comp.ServerScoreboard.Value, out var scores))
            return false;

        highScoreEntries = GetSortedHighscores(scores);
        maxScores = GetMaxEntries(ent.Comp.ServerScoreboard.Value);
        return true;
    }

    private int? GetServerPlacement(
        Dictionary<ProtoId<ArcadeScoreboardPrototype>, List<ArcadeHighScoreEntry>> serverScores,
        ProtoId<ArcadeScoreboardPrototype>? scoreboard,
        ArcadeHighScoreEntry entry)
    {
        if (scoreboard == null || !serverScores.TryGetValue(scoreboard.Value, out var scores))
            return null;

        return TryInsertIntoList(scores, entry, GetMaxEntries(scoreboard));
    }

    private int GetMaxEntries(Entity<ArcadeScoreboardComponent> ent)
    {
        return ent.Comp.MaxEntriesOverride
            ?? GetMaxEntries(ent.Comp.ServerScoreboard);
    }

    private int GetMaxEntries(ProtoId<ArcadeScoreboardPrototype>? protoId)
    {
        _prototypeManager.TryIndex(protoId, out var scoreboard);

        return scoreboard?.MaxEntries
            ?? _config.GetCVar(CCVars.FallbackScoreboardEntriesCount);
    }

    private static List<ArcadeHighScoreEntry> GetSortedHighscores(List<ArcadeHighScoreEntry> highScoreEntries)
    {
        var result = highScoreEntries.ShallowClone();
        result.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));
        return result;
    }

    private static int? TryInsertIntoList(List<ArcadeHighScoreEntry> highScoreEntries,
        ArcadeHighScoreEntry entry,
        int maxEntries)
    {
        // Maximum number of entries.
        // We can just add the score to the list and return its placement.
        if (highScoreEntries.Count < maxEntries)
        {
            highScoreEntries.Add(entry);
            return GetPlacement(highScoreEntries, entry);
        }

        // Otherwise: If the score is lower than the lowest top entry, it does not have a placement.
        if (highScoreEntries.Min(e => e.Score) >= entry.Score)
            return null;

        // If this is a new top score, then we add it to the list, then remove the new lowest score from the list.
        var lowestHighscore = highScoreEntries.Min();
        if (lowestHighscore == null)
            return null;

        highScoreEntries.Remove(lowestHighscore);
        highScoreEntries.Add(entry);

        return GetPlacement(highScoreEntries, entry);
    }

    private static int? GetPlacement(List<ArcadeHighScoreEntry> highScoreEntries, ArcadeHighScoreEntry entry)
    {
        int? placement = null;
        if (highScoreEntries.Contains(entry))
        {
            highScoreEntries.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));
            placement = 1 + highScoreEntries.IndexOf(entry);
        }

        return placement;
    }
}

/// <summary>
/// A struct representing the leaderboard placements for a submitted score.
/// </summary>
/// <remarks>
/// Placements only have int values if they are "top scores"; otherwise, it will be null.
/// </remarks>
public readonly struct HighScorePlacement
{
    /// <summary>
    /// The score's placement across all rounds.
    /// </summary>
    public readonly int? GlobalPlacement;

    /// <summary>
    /// The score's placement for this round.
    /// </summary>
    public readonly int? LocalPlacement;

    /// <summary>
    /// The score's placement for this specific arcade machine.
    /// </summary>
    public readonly int? MachinePlacement;

    public HighScorePlacement(int? globalPlacement, int? localPlacement, int? machinePlacement)
    {
        GlobalPlacement = globalPlacement;
        LocalPlacement = localPlacement;
        MachinePlacement = machinePlacement;
    }
}

/// <summary>
/// Called on the arcade machine when a high score placement is submitted.
/// </summary>
/// <param name="Player">The entity playing the machine.</param>
/// <param name="Placements">The local and global placements of the player's score</param>
[ByRefEvent]
public record struct ArcadeScorePlacementSubmittedEvent(EntityUid Player, int Score, HighScorePlacement Placements)
{
    /// <summary>
    /// The entity that submitted this score.
    /// </summary>
    public EntityUid Player = Player;

    /// <summary>
    /// The score submitted by the player.
    /// </summary>
    public int Score = Score;

    /// <summary>
    /// How this player's score ranks in the scoreboards for this machine.
    /// </summary>
    public HighScorePlacement Placements = Placements;
}
