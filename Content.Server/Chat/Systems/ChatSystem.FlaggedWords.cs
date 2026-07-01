using System.Text.RegularExpressions;
using Content.Server.Administration.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Content.Server.Database;
using Content.Shared.Database;
using System.Threading.Tasks;

namespace Content.Server.Chat.Systems;



public sealed partial class ChatSystem
{
    [Dependency] private BwoinkSystem _bwoink = default!;
    [Dependency] private IGameTiming _timing = default!;
    private float _flaggedWordAhelpCooldown = 30f;
    private readonly Dictionary<NetUserId, TimeSpan> _lastFlaggedWordAhelp = new();
    private bool _flaggedWordAhelpEnabled;
    private Regex? _flaggedWordRegex;
    private readonly Dictionary<string, FlaggedWord> _flaggedWordsByWord =
        new(StringComparer.OrdinalIgnoreCase);
    private async void LoadFlaggedWordsFromDatabase()
    {
        try
        {
            var words = await _db.GetFlaggedWordsAsync();
            RebuildFlaggedWordRegex(words);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load flagged words from database: {e}");
            _flaggedWordRegex = null;
        }
    }

    private void RebuildFlaggedWordRegex(IEnumerable<FlaggedWord> entries)
    {
        _flaggedWordsByWord.Clear();

        var partialMatches = new List<string>();
        var wholeMatches = new List<string>();

        foreach (var entry in entries)
        {
            if (!entry.Enabled)
                continue;

            var word = entry.Word.Trim();
            if (string.IsNullOrWhiteSpace(word))
                continue;

            _flaggedWordsByWord[word] = entry;

            var escaped = Regex.Escape(word);

            if (entry.FlagPartialMatches)
            {
                partialMatches.Add(escaped);
            }
            else
            {
                wholeMatches.Add(escaped);
            }
        }

        // Prefer higher severity words since we take the first match
        partialMatches.Sort((a, b) => _flaggedWordsByWord[b].Severity.CompareTo(_flaggedWordsByWord[a].Severity));
        wholeMatches.Sort((a, b) => _flaggedWordsByWord[b].Severity.CompareTo(_flaggedWordsByWord[a].Severity));

        var patterns = new List<string>();

        if (wholeMatches.Count > 0)
        {
            patterns.Add(
                $@"(?<!\p{{L}}|\p{{N}})({string.Join("|", wholeMatches)})(?!\p{{L}}|\p{{N}})");
        }

        if (partialMatches.Count > 0)
        {
            patterns.Add($"({string.Join("|", partialMatches)})");
        }

        _flaggedWordRegex = patterns.Count == 0
            ? null
            : new Regex(
                string.Join("|", patterns),
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
    private bool IsFlaggedWordAhelpOnCooldown(NetUserId userId)
    {
        if (_flaggedWordAhelpCooldown <= 0f)
            return false;

        var now = _timing.CurTime;
        var cooldown = TimeSpan.FromSeconds(_flaggedWordAhelpCooldown);

        if (_lastFlaggedWordAhelp.TryGetValue(userId, out var lastAlert) &&
            now - lastAlert < cooldown)
        {
            return true;
        }

        _lastFlaggedWordAhelp[userId] = now;
        return false;
    }

    private string CheckFlaggedWords(EntityUid source, ICommonSession? player, string originalMessage)
    {
        if (!_flaggedWordAhelpEnabled || player == null || _flaggedWordRegex == null)
            return "";

        var match = _flaggedWordRegex.Match(originalMessage);
        if (!match.Success)
            return "";

        if (IsFlaggedWordAhelpOnCooldown(player.UserId))
            return "";

        var report = Loc.GetString("admin-flagged-word", ("flagged", match.Value), ("original", originalMessage));

        _adminLogger.Add(LogType.Chat, LogImpact.Medium, $"{source} triggered flagged word check with word {match.Value} saying: {originalMessage}.");

        if (_flaggedWordsByWord[match.Value].Severity >= FlaggedWordSeverity.High)
        {
            _bwoink.SendAutomatedPlayerAHelp(player, report);
        }
        return match.Value;
    }

    public void ReloadFlaggedWords()
    {
        LoadFlaggedWordsFromDatabase();
    }
}
