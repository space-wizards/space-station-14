using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class RussianAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // Sound replacement regexes
    private static readonly Regex ThToZVowelRegex = new(@"\bTh(?=[aeiou])", RegexOptions.Compiled);
    private static readonly Regex ThToZWordsRegex = new(@"Th(?=at|is|ese|ose|ey|em|an)", RegexOptions.Compiled);
    private static readonly Regex AllCapsThToZVowelRegex = new(@"\bTH(?=[AEIOU])", RegexOptions.Compiled);
    private static readonly Regex AllCapsThToZWordsRegex = new(@"TH(?=AT|IS|ESE|OSE|EY|EM|AN)", RegexOptions.Compiled);
    private static readonly Regex LowercaseThToZVowelRegex = new(@"\bth(?=[aeiou])", RegexOptions.Compiled);
    private static readonly Regex LowercaseThToZWordsRegex = new(@"th(?=at|is|ese|ose|ey|em|an)", RegexOptions.Compiled);
    private static readonly Regex CToKCapitalRegex = new(@"\bC", RegexOptions.Compiled);
    private static readonly Regex CToKLowercaseRegex = new(@"\bc", RegexOptions.Compiled);
    private static readonly Regex WToVCapitalRegex = new(@"\bW", RegexOptions.Compiled);
    private static readonly Regex WToVLowercaseRegex = new(@"\bw", RegexOptions.Compiled);
    private static readonly Regex DentalTInVowelsRegex = new(@"(?<=[aeiouAEIOU])t(?=[aeiouAEIOU])", RegexOptions.Compiled);
    private static readonly Regex EeRegex = new(@"ee", RegexOptions.Compiled);

    // Grammar replacement regexes
    private static readonly Regex TheLowercaseRegex = new(@"\bthe\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TheCapitalRegex = new(@"\bThe\b", RegexOptions.Compiled);
    private static readonly Regex ALowercaseRegex = new(@"\ba\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ACapitalRegex = new(@"\bA\b", RegexOptions.Compiled);
    private static readonly Regex AnLowercaseRegex = new(@"\ban\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AnCapitalRegex = new(@"\bAn\b", RegexOptions.Compiled);
    private static readonly Regex IsLowercaseRegex = new(@"\bis\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IsCapitalRegex = new(@"\bIs\b", RegexOptions.Compiled);
    private static readonly Regex AreLowercaseRegex = new(@"\bare\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AreCapitalRegex = new(@"\bAre\b", RegexOptions.Compiled);
    private static readonly Regex IAmLowercaseRegex = new(@"\bI am\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IAmCapitalRegex = new(@"\bI Am\b", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex WordBoundaryRegex = new(@"\b\w+\b", RegexOptions.Compiled);

    // Homoglyph mapping for visually similar Cyrillic letters, these are limited to 2 per word for readability
    // TODO: (Pending Chat Refactor) This was intentionally kept to the absolute bare minimum to ensure that it remains readable. If we want to expand this, it should have an accessibility option to disable per client. Until then, keep it simple.
    private static readonly Dictionary<char, char> HomoglyphMap = new()
    {
        { 'K', 'К' }, { 'k', 'к' },
        { 'm', 'м' },
        { 'n', 'п'},
        { 'r', 'г'},
        { 'Y', 'У' }
    };

    private static readonly Regex TovarischRegex = new(@"\btovarisch\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override void Initialize()
    {
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>(OnAccent);
    }

    // Applies Russian accent to a message
    public string Accentuate(string message, RussianAccentComponent component)
    {
        var accentedMessage = _replacement.ApplyReplacements(message, "russian");
        accentedMessage = ApplyKomradeReplacement(accentedMessage, component);
        accentedMessage = ApplyGrammarRules(accentedMessage, component);
        accentedMessage = ApplySoundReplacements(accentedMessage);
        accentedMessage = ApplyHomoglyphs(accentedMessage);
        return accentedMessage;
    }

    // Randomly replaces 'tovarisch' with 'Komrade' while preserving capitalization.
    // TODO: The ReplacementAccentSystem REALLY should have random replacements built-in.
    private string ApplyKomradeReplacement(string message, RussianAccentComponent component)
    {
        return TovarischRegex.Replace(message, match =>
        {
            if (!_random.Prob(component.KomradeReplacementChance))
                return match.Value;
            var original = match.Value;
            if (IsAllUpperCase(original))
                return "KOMRADE";
            if (IsCapitalized(original))
                return "Komrade";
            return "komrade";
        });
    }

    private static bool IsAllUpperCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (var c in text)
            if (char.IsLetter(c) && !char.IsUpper(c)) return false;
        return true;
    }

    private static bool IsCapitalized(string text)
    {
        if (string.IsNullOrEmpty(text) || !char.IsLetter(text[0])) return false;
        if (!char.IsUpper(text[0])) return false;
        for (int i = 1; i < text.Length; i++)
            if (char.IsLetter(text[i]) && !char.IsLower(text[i])) return false;
        return true;
    }

    // Applies sound-level replacements to simulate Russian accent phonetics.
    private string ApplySoundReplacements(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var result = message;

        // Apply TH replacements (grouped by case)
        result = ThToZVowelRegex.Replace(result, "Z");
        result = ThToZWordsRegex.Replace(result, "Z");
        result = AllCapsThToZVowelRegex.Replace(result, "Z");
        result = AllCapsThToZWordsRegex.Replace(result, "Z");
        result = LowercaseThToZVowelRegex.Replace(result, "z");
        result = LowercaseThToZWordsRegex.Replace(result, "z");

        // Apply other consonant replacements
        result = CToKCapitalRegex.Replace(result, "K");
        result = CToKLowercaseRegex.Replace(result, "k");
        result = WToVCapitalRegex.Replace(result, "V");
        result = WToVLowercaseRegex.Replace(result, "v");

        // Apply vowel and other sound changes
        result = DentalTInVowelsRegex.Replace(result, "th");
        result = EeRegex.Replace(result, "i");

        // Restore capitalization
        if (result.Length > 0 && message.Length > 0 &&
            char.IsLetter(message[0]) && char.IsLower(result[0]) && char.IsUpper(message[0]))
        {
            result = char.ToUpper(result[0]) + result.Substring(1);
        }

        return result;
    }

    // Applies grammar rules typical of Russian-accented English, such as article and verb removal.
    private string ApplyGrammarRules(string message, RussianAccentComponent component)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var wasFirstLetterCapitalized = char.IsUpper(message[0]);

        // Early exit if its not long enough
        var wordCount = message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount <= 3)
        {
            return message;
        }

        // Instead of checking per word we're just gonna check for the whole message.
        if (!_random.Prob(component.ArticleRemovalChance))
        {
            return message;
        }

        var result = message;

        // If a message starts with any of these we remove em if ArticalRemovalChance passes. This is more optimized than doing a regex check.
        if (result.StartsWith("The ", StringComparison.Ordinal))
            result = result.Substring(4);
        else if (result.StartsWith("THE ", StringComparison.Ordinal))
            result = result.Substring(4);
        else if (result.StartsWith("A ", StringComparison.Ordinal))
            result = result.Substring(2);
        else if (result.StartsWith("An ", StringComparison.Ordinal))
            result = result.Substring(3);
        else
        {
            // Apply regex replacements for articles elsewhere in the message
            result = TheLowercaseRegex.Replace(result, "");
            result = TheCapitalRegex.Replace(result, "");
            result = ALowercaseRegex.Replace(result, "");
            result = ACapitalRegex.Replace(result, "");
            result = AnLowercaseRegex.Replace(result, "");
            result = AnCapitalRegex.Replace(result, "");
        }

        // Remove verbs
        result = IsLowercaseRegex.Replace(result, "");
        result = IsCapitalRegex.Replace(result, "");
        result = AreLowercaseRegex.Replace(result, "");
        result = AreCapitalRegex.Replace(result, "");

        // Simplify "I am" to "I"
        result = IAmLowercaseRegex.Replace(result, "I");
        result = IAmCapitalRegex.Replace(result, "I");

        // Clean up whitespace
        result = WhitespaceRegex.Replace(result.Trim(), " ");

        // Restore capitalization
        if (wasFirstLetterCapitalized && !string.IsNullOrEmpty(result) && char.IsLetter(result[0]) && char.IsLower(result[0]))
        {
            result = char.ToUpper(result[0]) + result.Substring(1);
        }

        return result;
    }

    private void OnAccent(EntityUid uid, RussianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }

    // Applies homoglyph replacements for visual accent, limiting to 2 per word for readability
    private string ApplyHomoglyphs(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var words = message.Split(' ');
        var hasChanges = false;

        // Check if any changes are needed before anything.
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            int potentialReplacements = 0;
            for (int j = 0; j < word.Length && potentialReplacements < 2; j++)
            {
                if (HomoglyphMap.ContainsKey(word[j]))
                {
                    potentialReplacements++;
                    hasChanges = true;
                }
            }
        }

        if (!hasChanges)
            return message;

        // Apply changes if any are actually needed
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var chars = word.ToCharArray();
            int replaced = 0;

            for (int j = 0; j < chars.Length && replaced < 2; j++)
            {
                if (HomoglyphMap.TryGetValue(chars[j], out var glyph))
                {
                    chars[j] = glyph;
                    replaced++;
                }
            }

            if (replaced > 0)
                words[i] = new string(chars);
        }

        return string.Join(' ', words);
    }

    /// <summary>
    /// Applies only homoglyph replacements to words that would be replaced by the Russian accent system.
    /// Ignores all other accent rules (no grammar or sound changes).
    /// </summary>
    public string ApplyReplacementsHomoglyphOnly(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var replacedMessage = _replacement.ApplyReplacements(message, "russian");

        // If no replacements occurred, return original message
        if (string.Equals(message, replacedMessage, StringComparison.Ordinal))
            return message;

        // Use regex to find word boundaries and handle punctuation properly
        var originalMatches = WordBoundaryRegex.Matches(message).ToList();
        var replacedMatches = WordBoundaryRegex.Matches(replacedMessage).ToList();

        // If word count doesn't match, fall back to simple approach
        if (originalMatches.Count != replacedMatches.Count)
        {
            return ApplyHomoglyphs(replacedMessage);
        }

        // Build result by replacing only changed words with homoglyph versions
        var result = new System.Text.StringBuilder(message);
        var offset = 0;

        for (int i = 0; i < originalMatches.Count && i < replacedMatches.Count; i++)
        {
            var originalWord = originalMatches[i].Value;
            var replacedWord = replacedMatches[i].Value;

            if (!string.Equals(originalWord, replacedWord, StringComparison.Ordinal))
            {
                var homoglyphWord = ApplyHomoglyphs(replacedWord);
                var originalPos = originalMatches[i].Index + offset;

                result.Remove(originalPos, originalWord.Length);
                result.Insert(originalPos, homoglyphWord);

                offset += homoglyphWord.Length - originalWord.Length;
            }
        }

        return result.ToString();
    }
}
