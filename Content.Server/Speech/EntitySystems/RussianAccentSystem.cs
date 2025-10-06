using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class RussianAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // Cached regex patterns
    private static readonly Regex TovarischRegex = new(@"\btovarisch\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // I HATE DOING REGEXES WHY DID I PUT MYSELF THROUGH THIS.
    // Sound replacement regexes
    private static readonly Regex ThToZVowelRegex = new(@"\bTh(?=[aeiou])", RegexOptions.Compiled);
    private static readonly Regex ThToZWordsRegex = new(@"Th(?=at|is|ese|ose|ey|em|an)", RegexOptions.Compiled);
    private static readonly Regex ThToSWordsRegex = new(@"Th(?=ink|ing|ought|ough|ick|in)", RegexOptions.Compiled);
    private static readonly Regex AllCapsThToZVowelRegex = new(@"\bTH(?=[AEIOU])", RegexOptions.Compiled);
    private static readonly Regex AllCapsThToZWordsRegex = new(@"TH(?=AT|IS|ESE|OSE|EY|EM|AN)", RegexOptions.Compiled);
    private static readonly Regex AllCapsThToSWordsRegex = new(@"TH(?=INK|ING|OUGHT|OUGH|ICK|IN)", RegexOptions.Compiled);
    private static readonly Regex LowercaseThToZVowelRegex = new(@"\bth(?=[aeiou])", RegexOptions.Compiled);
    private static readonly Regex LowercaseThToZWordsRegex = new(@"th(?=at|is|ese|ose|ey|em|an)", RegexOptions.Compiled);
    private static readonly Regex LowercaseThToSWordsRegex = new(@"th(?=ink|ing|ought|ough|ick|in)", RegexOptions.Compiled);
    private static readonly Regex CToKCapitalRegex = new(@"\bC(?=[aeiouAEIOU])", RegexOptions.Compiled);
    private static readonly Regex CToKLowercaseRegex = new(@"\bc(?=[aeiou])", RegexOptions.Compiled);
    private static readonly Regex CToKMiddleCapitalRegex = new(@"C(?=[aeiouAEIOU])", RegexOptions.Compiled);
    private static readonly Regex CToKMiddleLowercaseRegex = new(@"c(?=[aeiou])", RegexOptions.Compiled);
    private static readonly Regex WToVCapitalRegex = new(@"\bW", RegexOptions.Compiled);
    private static readonly Regex WToVLowercaseRegex = new(@"\bw", RegexOptions.Compiled);

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

    public override void Initialize()
    {
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>(OnAccent);
    }

    /// Applies Russian accent to a message by performing word replacements, grammar rules, and sound changes.
    public string Accentuate(string message, RussianAccentComponent component)
    {
        var accentedMessage = _replacement.ApplyReplacements(message, "russian");

        // Randomly replace 'tovarisch' with 'Komrade' (20% chance per instance, matching capitalization)
        accentedMessage = ApplyKomradeReplacement(accentedMessage);

        accentedMessage = ApplyGrammarRules(accentedMessage, component);
        accentedMessage = ApplySoundReplacements(accentedMessage);

        return accentedMessage;
    }

    /// Randomly replaces 'tovarisch' with 'Komrade' while preserving capitalization.
    /// TODO: The ReplacementAccentSystem REALLY should have random replacements this built-in.
    private string ApplyKomradeReplacement(string message)
    {
        return TovarischRegex.Replace(message, match =>
        {
            if (!_random.Prob(0.2f))
                return match.Value;

            var original = match.Value;

            if (IsAllUpperCase(original))
                return "KOMRADE";

            if (IsCapitalized(original))
                return "Komrade";

            return "komrade";
        });
    }

    /// Checks if a string is all uppercase letters.
    private static bool IsAllUpperCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsLetter(text[i]) && !char.IsUpper(text[i]))
                return false;
        }
        return true;
    }

    /// Checks if a string is capitalized (first letter uppercase, rest lowercase).
    private static bool IsCapitalized(string text)
    {
        if (string.IsNullOrEmpty(text) || !char.IsLetter(text[0]))
            return false;

        if (!char.IsUpper(text[0]))
            return false;

        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsLetter(text[i]) && !char.IsLower(text[i]))
                return false;
        }
        return true;
    }

    /// Applies sound-level replacements to simulate Russian accent phonetics.
    private string ApplySoundReplacements(string message)
    {
        var result = message;
        // I HATE DOING REGEX WHY DID I PUT MYSELF THROUGH THIS
        // Capitalized "Th" to "Z" or "S" depending on context
        result = ThToZVowelRegex.Replace(result, "Z");
        result = ThToZWordsRegex.Replace(result, "Z");
        result = ThToSWordsRegex.Replace(result, "S");

        // All-caps "TH" to "Z" or "S"
        result = AllCapsThToZVowelRegex.Replace(result, "Z");
        result = AllCapsThToZWordsRegex.Replace(result, "Z");
        result = AllCapsThToSWordsRegex.Replace(result, "S");

        // Lowercase "th" to "z" or "s"
        result = LowercaseThToZVowelRegex.Replace(result, "z");
        result = LowercaseThToZWordsRegex.Replace(result, "z");
        result = LowercaseThToSWordsRegex.Replace(result, "s");

        // "c" to "k" for hard c sounds
        result = CToKCapitalRegex.Replace(result, "K");
        result = CToKLowercaseRegex.Replace(result, "k");
        result = CToKMiddleCapitalRegex.Replace(result, "K");
        result = CToKMiddleLowercaseRegex.Replace(result, "k");

        // "w" to "v"
        result = WToVCapitalRegex.Replace(result, "V");
        result = WToVLowercaseRegex.Replace(result, "v");

        // make sure to capitalize first character if original message started with a lowercase letter
        if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(message) && char.IsLetter(message[0]) && char.IsLower(result[0]))
        {
            result = char.ToUpper(result[0]) + result[1..];
        }

        return result;
    }

    /// Applies grammar rules typical of Russian-accented English, such as article and verb removal.
    private string ApplyGrammarRules(string message, RussianAccentComponent component)
    {
        var result = message;
        var wasFirstLetterCapitalized = !string.IsNullOrEmpty(message) && char.IsUpper(message[0]);

        // Determine if the message is a short phrase (<= 3 words)
        var wordCount = result.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var isShortPhrase = wordCount <= 3;

        // Remove "the" articles (with chance, not for short phrases)
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            if (result.StartsWith("The "))
                result = result[4..];
            else if (result.StartsWith("THE "))
                result = result[4..];
            else
            {
                result = TheLowercaseRegex.Replace(result, "");
                result = TheCapitalRegex.Replace(result, "");
            }
        }

        // Remove "a"/"an" articles
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            if (result.StartsWith("A "))
                result = result[2..];
            else if (result.StartsWith("An "))
                result = result[3..];
            else
            {
                result = ALowercaseRegex.Replace(result, "");
                result = ACapitalRegex.Replace(result, "");
                result = AnLowercaseRegex.Replace(result, "");
                result = AnCapitalRegex.Replace(result, "");
            }
        }

        // Remove present tense "to be" verbs
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            result = IsLowercaseRegex.Replace(result, "");
            result = IsCapitalRegex.Replace(result, "");
            result = AreLowercaseRegex.Replace(result, "");
            result = AreCapitalRegex.Replace(result, "");
        }

        // Simplify "I am" to "I"
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            result = IAmLowercaseRegex.Replace(result, "I");
            result = IAmCapitalRegex.Replace(result, "I");
        }

        // Clean up extra spaces
        result = WhitespaceRegex.Replace(result, " ");
        result = result.Trim();

        if (wasFirstLetterCapitalized && !string.IsNullOrEmpty(result) && char.IsLetter(result[0]) && char.IsLower(result[0]))
        {
            result = char.ToUpper(result[0]) + result[1..];
        }

        return result;
    }

    private void OnAccent(EntityUid uid, RussianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
