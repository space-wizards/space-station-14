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

    // Homoglyph mapping for visually similar Cyrillic letters
    private static readonly Dictionary<char, char> HomoglyphMap = new()
    {
        { 'K', 'К' }, { 'k', 'к' },
        { 'm', 'м' },
        { 'Y', 'У' }
    };

    private static readonly Regex TovarischRegex = new(@"\btovarisch\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override void Initialize()
    {
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>(OnAccent);
    }

    // Applies Russian accent to a message by performing word replacements, grammar rules, and sound changes.
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
        var result = message;
        result = ThToZVowelRegex.Replace(result, "Z");
        result = ThToZWordsRegex.Replace(result, "Z");
        result = AllCapsThToZVowelRegex.Replace(result, "Z");
        result = AllCapsThToZWordsRegex.Replace(result, "Z");
        result = LowercaseThToZVowelRegex.Replace(result, "z");
        result = LowercaseThToZWordsRegex.Replace(result, "z");
        result = CToKCapitalRegex.Replace(result, "K");
        result = CToKLowercaseRegex.Replace(result, "k");
        result = WToVCapitalRegex.Replace(result, "V");
        result = WToVLowercaseRegex.Replace(result, "v");
        result = DentalTInVowelsRegex.Replace(result, "th");
        result = EeRegex.Replace(result, "i");
        if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(message) && char.IsLetter(message[0]) && char.IsLower(result[0]))
            result = char.ToUpper(result[0]) + result[1..];
        return result;
    }

    // Applies grammar rules typical of Russian-accented English, such as article and verb removal.
    private string ApplyGrammarRules(string message, RussianAccentComponent component)
    {
        var result = message;
        var wasFirstLetterCapitalized = !string.IsNullOrEmpty(message) && char.IsUpper(message[0]);
        var wordCount = result.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var isShortPhrase = wordCount <= 3;
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            if (result.StartsWith("The ")) result = result[4..];
            else if (result.StartsWith("THE ")) result = result[4..];
            else
            {
                result = TheLowercaseRegex.Replace(result, "");
                result = TheCapitalRegex.Replace(result, "");
            }
        }
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            if (result.StartsWith("A ")) result = result[2..];
            else if (result.StartsWith("An ")) result = result[3..];
            else
            {
                result = ALowercaseRegex.Replace(result, "");
                result = ACapitalRegex.Replace(result, "");
                result = AnLowercaseRegex.Replace(result, "");
                result = AnCapitalRegex.Replace(result, "");
            }
        }
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            result = IsLowercaseRegex.Replace(result, "");
            result = IsCapitalRegex.Replace(result, "");
            result = AreLowercaseRegex.Replace(result, "");
            result = AreCapitalRegex.Replace(result, "");
        }
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            result = IAmLowercaseRegex.Replace(result, "I");
            result = IAmCapitalRegex.Replace(result, "I");
        }
        result = WhitespaceRegex.Replace(result, " ");
        result = result.Trim();
        if (wasFirstLetterCapitalized && !string.IsNullOrEmpty(result) && char.IsLetter(result[0]) && char.IsLower(result[0]))
            result = char.ToUpper(result[0]) + result[1..];
        return result;
    }

    private void OnAccent(EntityUid uid, RussianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }

    // Applies homoglyph replacements for visual accent, limiting to 2 per word for readability
    private string ApplyHomoglyphs(string message)
    {
        var words = message.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            int replaced = 0;
            var chars = words[i].ToCharArray();
            for (int j = 0; j < chars.Length; j++)
            {
                if (HomoglyphMap.TryGetValue(chars[j], out var glyph) && replaced < 2)
                {
                    chars[j] = glyph;
                    replaced++;
                }
            }
            words[i] = new string(chars);
        }
        return string.Join(' ', words);
    }
}
