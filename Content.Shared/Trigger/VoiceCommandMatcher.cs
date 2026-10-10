using System.Text;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger;

/// <summary>
/// Trigram matching for spoken voice commands and keyphrases.
/// </summary>
public static class VoiceCommandMatcher
{
    public const string VoiceCommandQuantityWordsLoc = "voice-command-quantity-words";

    public static List<VoiceCommandCandidate> BuildVoiceCommandCandidates(IReadOnlyDictionary<string, string> triggers)
    {
        var candidates = new List<VoiceCommandCandidate>(triggers.Count);
        foreach (var (phrase, tag) in triggers)
        {
            var trigrams = Trigrams(NormalizePhrase(phrase));
            if (trigrams.Count == 0)
                continue;

            candidates.Add(new VoiceCommandCandidate
            {
                Tag = tag,
                Trigrams = trigrams,
            });
        }
        return candidates;
    }

    // Matches spoken text to a tag, pulling a leading count into quantity (default 1).
    public static bool TryMatchVoiceCommand(VoiceCommandsComponent comp, string message, IReadOnlyDictionary<string, int> spokenDigits, out string tag, out int quantity)
    {
        tag = string.Empty;
        quantity = 1;

        if (comp.Candidates.Count == 0)
            return false;

        var tokens = message.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return false;

        var phraseStart = 0;
        if (tokens.Length > 1 && TryParseCount(tokens[0], spokenDigits, out var count))
        {
            if (count <= 0)
                return false;

            quantity = count;
            phraseStart = 1;
        }

        var threshold = ClampMatchThreshold(comp.MatchThreshold);

        var match = MatchPhrase(comp.Candidates, tokens[phraseStart..], threshold);
        if (match == null)
        {
            quantity = 1;
            return false;
        }

        tag = match.Tag;
        return true;
    }

    // Strips the keyphrase out, fuzzily if enabled, leaving the command text.
    public static bool TryExtractKeyphrase(string message, string keyphrase, List<VoiceCommandCandidate> keyphraseCandidates, bool fuzzyMatch, float threshold, out string remainder)
    {
        var index = message.IndexOf(keyphrase, StringComparison.InvariantCultureIgnoreCase);
        if (index >= 0)
        {
            remainder = TrimCommandRemainder(message.Remove(index, keyphrase.Length));
            return true;
        }

        if (fuzzyMatch && keyphraseCandidates.Count > 0
            && TryFuzzyExtractKeyphrase(message, keyphrase, keyphraseCandidates, threshold, out var fuzzy))
        {
            remainder = TrimCommandRemainder(fuzzy);
            return true;
        }

        remainder = string.Empty;
        return false;
    }

    // Builds the candidate inline, for tests and one-offs.
    public static bool TryExtractKeyphrase(string message, string keyphrase, bool fuzzyMatch, float threshold, out string remainder)
    {
        var candidates = fuzzyMatch
            ? BuildVoiceCommandCandidates(new Dictionary<string, string> { [keyphrase] = string.Empty })
            : new List<VoiceCommandCandidate>();
        return TryExtractKeyphrase(message, keyphrase, candidates, fuzzyMatch, threshold, out remainder);
    }

    public static Dictionary<string, int> BuildSpokenDigits(Func<string, string> getString)
    {
        return BuildSpokenDigitsFromWords(getString(VoiceCommandQuantityWordsLoc));
    }

    private static bool TryFuzzyExtractKeyphrase(string message, string keyphrase, List<VoiceCommandCandidate> candidates, float threshold, out string remainder)
    {
        remainder = string.Empty;

        var keyTokens = keyphrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var msgTokens = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (keyTokens.Length == 0 || msgTokens.Length < keyTokens.Length)
            return false;

        var minScore = ClampMatchThreshold(threshold);
        for (var start = 0; start <= msgTokens.Length - keyTokens.Length; start++)
        {
            if (MatchPhrase(candidates, msgTokens[start..(start + keyTokens.Length)], minScore) == null)
                continue;

            var kept = new List<string>(msgTokens.Length - keyTokens.Length);
            for (var i = 0; i < msgTokens.Length; i++)
            {
                if (i < start || i >= start + keyTokens.Length)
                    kept.Add(msgTokens[i]);
            }
            remainder = string.Join(' ', kept);
            return true;
        }

        return false;
    }

    private static string TrimCommandRemainder(string remainder)
    {
        var start = 0;
        while (start < remainder.Length && !char.IsLetterOrDigit(remainder[start]))
            start++;

        var end = remainder.Length;
        while (end > start && !char.IsLetterOrDigit(remainder[end - 1]))
            end--;

        return remainder[start..end];
    }

    private static VoiceCommandCandidate? MatchPhrase(
        List<VoiceCommandCandidate> candidates,
        IReadOnlyList<string> phraseTokens,
        float threshold)
    {
        if (phraseTokens.Count == 0)
            return null;

        var phrase = NormalizePhrase(string.Join(' ', phraseTokens));
        if (phrase.Length == 0)
            return null;

        var variants = GetPhraseTrigrams(phrase);
        if (variants.Count == 0)
            return null;

        // Prefer triggers the speaker said in full, then fall back to triggers that contain the speech.
        return BestMatch(candidates, variants, threshold, allowPartialTrigger: false)
               ?? BestMatch(candidates, variants, threshold, allowPartialTrigger: true);
    }

    private static VoiceCommandCandidate? BestMatch(
        List<VoiceCommandCandidate> candidates,
        List<HashSet<string>> messageTrigramVariants,
        float threshold,
        bool allowPartialTrigger)
    {
        VoiceCommandCandidate? best = null;
        var bestCovered = -1f;
        var bestSaid = -1f;

        foreach (var candidate in candidates)
        {
            var said = 0f;
            var covered = 0f;
            foreach (var v in messageTrigramVariants)
            {
                var s = TrigramOverlap(candidate.Trigrams, v);
                if (s > said) said = s;
                var c = TrigramOverlap(v, candidate.Trigrams);
                if (c > covered) covered = c;
            }

            var gate = allowPartialTrigger ? covered : said;
            if (gate < threshold)
                continue;

            // Rank by coverage, then by how fully the trigger was said, then by shortest, so
            // "beaker" wins over the superset "large beaker".
            if (best == null
                || covered > bestCovered
                || (covered == bestCovered && said > bestSaid)
                || (covered == bestCovered && said == bestSaid && candidate.Trigrams.Count < best.Trigrams.Count))
            {
                best = candidate;
                bestCovered = covered;
                bestSaid = said;
            }
        }

        return best;
    }

    private static Dictionary<string, int> BuildSpokenDigitsFromWords(string localizedWords)
    {
        var words = localizedWords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var map = new Dictionary<string, int>(words.Length);
        for (var i = 0; i < words.Length; i++)
            map[words[i].ToLowerInvariant()] = i + 1;
        return map;
    }

    private static bool TryParseCount(string token, IReadOnlyDictionary<string, int> spokenDigits, out int count)
    {
        // Speech-to-text leaves punctuation stuck to the count, e.g. "five, popcorn".
        var end = token.Length;
        while (end > 0 && !char.IsLetterOrDigit(token[end - 1]))
            end--;
        token = token[..end];

        if (int.TryParse(token, out count))
            return true;
        return spokenDigits.TryGetValue(token.ToLowerInvariant(), out count);
    }

    // Pads with spaces so trigrams catch word boundaries.
    private static string NormalizePhrase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length + 2);
        sb.Append(' ');
        var prevSpace = true;
        foreach (var c in input)
        {
            char next;
            if (char.IsLetterOrDigit(c))
                next = char.ToLowerInvariant(c);
            else
                next = ' ';

            if (next == ' ')
            {
                if (prevSpace)
                    continue;
                prevSpace = true;
            }
            else
            {
                prevSpace = false;
            }
            sb.Append(next);
        }
        if (!prevSpace)
            sb.Append(' ');

        return sb.Length <= 2 ? string.Empty : sb.ToString();
    }

    // Expects an already normalized, padded string.
    private static HashSet<string> Trigrams(string padded)
    {
        if (padded.Length < 3)
            return new HashSet<string>();

        var set = new HashSet<string>(padded.Length - 2);
        for (var i = 0; i <= padded.Length - 3; i++)
            set.Add(padded.Substring(i, 3));
        return set;
    }

    private static List<HashSet<string>> GetPhraseTrigrams(string normalized)
    {
        var t1 = Trigrams(normalized);
        if (t1.Count == 0)
            return [];

        var variants = new List<HashSet<string>>(2) { t1 };

        var singular = SingularizeNormalizedPhrase(normalized);
        if (singular != normalized)
        {
            var t2 = Trigrams(singular);
            if (t2.Count > 0 && !t2.SetEquals(t1))
                variants.Add(t2);
        }

        return variants;
    }

    private static string SingularizeNormalizedPhrase(string normalized)
    {
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return normalized;

        var changed = false;
        for (var i = 0; i < words.Length; i++)
        {
            var singular = SingularizeWord(words[i]);
            if (singular == words[i])
                continue;

            words[i] = singular;
            changed = true;
        }

        return changed ? $" {string.Join(' ', words)} " : normalized;
    }

    private static string SingularizeWord(string word)
    {
        if (word.Length > 3 && word.EndsWith("ies", StringComparison.Ordinal))
            return word[..^3] + "y";

        // Plural added "es" to a consonant cluster.
        if (word.Length > 4
            && (word.EndsWith("ches", StringComparison.Ordinal)
                || word.EndsWith("shes", StringComparison.Ordinal)
                || word.EndsWith("xes", StringComparison.Ordinal)))
        {
            return word[..^2];
        }

        // Plain "s" plural. Length > 3 so short words (eggs, peas) still singularize, skip "ss" (glass) so it isn't mangled.
        if (word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal))
            return word[..^1];

        return word;
    }

    // A 0 threshold would let a zero-overlap candidate match anything, so floor it.
    private static float ClampMatchThreshold(float threshold) => Math.Clamp(threshold, float.Epsilon, 1f);

    // Overlap coefficient, not Jaccard: fraction of reference present in candidate.
    private static float TrigramOverlap(HashSet<string> reference, HashSet<string> candidate)
    {
        if (reference.Count == 0)
            return 0f;
        var intersect = 0;
        foreach (var tri in reference)
        {
            if (candidate.Contains(tri))
                intersect++;
        }
        return (float) intersect / reference.Count;
    }
}
