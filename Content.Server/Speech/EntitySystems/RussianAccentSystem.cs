using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class RussianAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>(OnAccent);
    }

    /// Applies Russian accent to a message by performing word replacements, grammar rules, and sound changes.
    public string Accentuate(string message, RussianAccentComponent component)
    {
        var accentedMessage = _replacement.ApplyReplacements(message, "russian");

        accentedMessage = ApplyGrammarRules(accentedMessage, component);
        accentedMessage = ApplySoundReplacements(accentedMessage);

        return accentedMessage;
    }

    /// Applies sound-level replacements to simulate Russian accent phonetics.
    private string ApplySoundReplacements(string message)
    {
        var result = message;
        // I HATE DOING REGEX WHY DID I PUT MYSELF THROUGH THIS
        // Capitalized "Th" to "Z" or "S" depending on context
        result = Regex.Replace(result, @"\bTh(?=[aeiou])", "Z", RegexOptions.None);
        result = Regex.Replace(result, @"Th(?=at|is|ese|ose|ey|em|an)", "Z", RegexOptions.None);
        result = Regex.Replace(result, @"Th(?=ink|ing|ought|ough|ick|in)", "S", RegexOptions.None);

        // All-caps "TH" to "Z" or "S"
        result = Regex.Replace(result, @"\bTH(?=[AEIOU])", "Z", RegexOptions.None);
        result = Regex.Replace(result, @"TH(?=AT|IS|ESE|OSE|EY|EM|AN)", "Z", RegexOptions.None);
        result = Regex.Replace(result, @"TH(?=INK|ING|OUGHT|OUGH|ICK|IN)", "S", RegexOptions.None);

        // Lowercase "th" to "z" or "s"
        result = Regex.Replace(result, @"\bth(?=[aeiou])", "z", RegexOptions.None);
        result = Regex.Replace(result, @"th(?=at|is|ese|ose|ey|em|an)", "z", RegexOptions.None);
        result = Regex.Replace(result, @"th(?=ink|ing|ought|ough|ick|in)", "s", RegexOptions.None);

        // "c" to "k" for hard c sounds
        result = Regex.Replace(result, @"\bC(?=[aeiouAEIOU])", "K", RegexOptions.None);
        result = Regex.Replace(result, @"\bc(?=[aeiou])", "k", RegexOptions.None);
        result = Regex.Replace(result, @"C(?=[aeiouAEIOU])", "K", RegexOptions.None);
        result = Regex.Replace(result, @"c(?=[aeiou])", "k", RegexOptions.None);

        // "w" to "v"
        result = Regex.Replace(result, @"\bW", "V", RegexOptions.None);
        result = Regex.Replace(result, @"\bw", "v", RegexOptions.None);

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
                result = Regex.Replace(result, @"\bthe\b", "", RegexOptions.IgnoreCase);
                result = Regex.Replace(result, @"\bThe\b", "", RegexOptions.None);
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
                result = Regex.Replace(result, @"\ba\b", "", RegexOptions.IgnoreCase);
                result = Regex.Replace(result, @"\bA\b", "", RegexOptions.None);
                result = Regex.Replace(result, @"\ban\b", "", RegexOptions.IgnoreCase);
                result = Regex.Replace(result, @"\bAn\b", "", RegexOptions.None);
            }
        }

        // Remove present tense "to be" verbs
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            result = Regex.Replace(result, @"\bis\b", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bIs\b", "", RegexOptions.None);
            result = Regex.Replace(result, @"\bare\b", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bAre\b", "", RegexOptions.None);
        }

        // Simplify "I am" to "I"
        if (_random.Prob(component.ArticleRemovalChance) && !isShortPhrase)
        {
            result = Regex.Replace(result, @"\bI am\b", "I", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bI Am\b", "I", RegexOptions.None);
        }

        // Clean up extra spaces
        result = Regex.Replace(result, @"\s+", " ");
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
