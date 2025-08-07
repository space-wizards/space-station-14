using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class SlowAccentSystem : AccentSystem<SlowAccentComponent>
{
    /// <summary>
    /// Matches whitespace characters or commas (with or without a space after them).
    /// </summary>
    private static readonly Regex WordEndings = new("\\s|, |,");

    /// <summary>
    /// Matches the end of the string only if the last character is a "word" character.
    /// </summary>
    private static readonly Regex NoFinalPunctuation = new("\\w\\z");

    public override string Accentuate(Entity<SlowAccentComponent>? entity, string message)
    {
        // Add... some... delay... between... each... word
        message = WordEndings.Replace(message, "... ");

        // Add "..." to the end, if the last character is part of a word...
        if (NoFinalPunctuation.IsMatch(message))
            message += "...";

        return message;
    }
}
