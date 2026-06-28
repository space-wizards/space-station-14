using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Mind.Components;
using Robust.Shared.RichText;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public sealed partial class BwoinkSystem
{
    [GeneratedRegex(@"\b")]
    private static partial Regex WordBoundRegex();

    [GeneratedRegex(@"^[()\-.\s]*$")]
    private static partial Regex IgnoredWordRegex();

    private int _startWordMinSize;

    private bool IsStartingWord(ReadOnlySpan<char> word)
    {
        return word.Length >= _startWordMinSize;
    }

    private readonly record struct NameMatchOption(string Name, EntityUid Entity)
    {
        public readonly string[] NameWords = WordBoundRegex().Split(Name);

        public bool WordMatches(ReadOnlySpan<char> word)
        {
            foreach (var w in NameWords)
            {
                if (IgnoredWordRegex().IsMatch(word))
                    continue;

                if (w.Equals(word, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private List<NameMatchOption> GetNameMatchOptions()
    {
        var q = EntityQueryEnumerator<MindContainerComponent>();
        var list = new List<NameMatchOption>();

        while (q.MoveNext(out var entity, out _))
        {
            list.Add(new NameMatchOption(Name(entity), entity));
        }

        return list;
    }

    private static string? CheckNextWord(ReadOnlySpan<string> words, ref int curWordIdx)
    {
        while (++curWordIdx < words.Length)
        {
            var word = words[curWordIdx];
            if (IgnoredWordRegex().IsMatch(word))
                continue;

            return word;
        }

        return null;
    }

    private async Task<string> GenerateNameLinks(string unEscapedMessage)
    {
        var nameMatchOptions = GetNameMatchOptions();

        // Can't be arsed to optimize this matching code? Multithread it!

        return await Task.Run(() =>
        {
            var output = new FormattedStringBuilder();
            var words = WordBoundRegex().Split(unEscapedMessage);

            for (var i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (!IsStartingWord(word) || IgnoredWordRegex().IsMatch(word))
                {
                    output.AppendText(word);
                    continue;
                }

                // Check if the word matches a single name we've collected.

                var candidates = nameMatchOptions
                    .Where(m => m.WordMatches(word))
                    .Take(_config.GetCVar(CCVars.AhelpMaxQuickInfoCandidates))
                    .ToArray();

                if (candidates.Length == 0)
                {
                    output.AppendText(word);
                    continue;
                }

                var takenWordIdx = i;

                // Name matches!
                // Try to check next words to nail down on full names etc.

                while (true)
                {
                    var curWordIdx = takenWordIdx;
                    if (CheckNextWord(words, ref curWordIdx) is not { } nextWord)
                        break;

                    var newCandidates = candidates.Where(m => m.WordMatches(nextWord)).ToArray();
                    if (newCandidates.Length == 0)
                    {
                        // Next word doesn't match anything. We've gotten as far as we can.
                        break;
                    }

                    candidates = newCandidates;
                    takenWordIdx = curWordIdx;
                }

                output.BeginTag("bold").FinishTagOpen();
                foreach (var pushWord in words[i..(takenWordIdx + 1)])
                {
                    output.AppendText(pushWord);
                }

                output.PopTag();
                var idsString = string.Join(',', candidates.Select(e => e.Entity));
                output.AppendText(" ");
                output.MakeCommandLinkTag(
                    Loc.GetString("bwoink-message-name-link"),
                    CommandParsing.EscapeCommand(QuickInfoShared.CommandName, idsString));

                i = takenWordIdx;
            }

            return output.ToString();
        });
    }
}
