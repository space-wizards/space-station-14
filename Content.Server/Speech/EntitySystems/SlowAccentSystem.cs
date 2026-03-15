using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class SlowAccentSystem : BaseAccentSystem<SlowAccentComponent>
{
    /// <summary>
    /// Matches whitespace characters or commas (with or without a space after them).
    /// </summary>
    private static readonly Regex WordEndings = new("\\s|, |,");

    /// <summary>
    /// Matches the end of the string only if the last character is a "word" character.
    /// </summary>
    private static readonly Regex NoFinalPunctuation = new("\\w\\z");

    public override string Accentuate(string message, Entity<SlowAccentComponent>? _)
    {
        // Add... some... delay... between... each... word
        message = WordEndings.Replace(message, "... ");

        // Add "..." to the end, if the last character is part of a word...
        if (NoFinalPunctuation.IsMatch(message))
            message += "...";

        return message;
    }
}
