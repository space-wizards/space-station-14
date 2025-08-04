using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

/// <summary>
/// System that gives the speaker a faux-French accent.
/// </summary>
public sealed class FrenchAccentSystem : AccentSystem<FrenchAccentComponent>
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexTh = new(@"th", RegexOptions.IgnoreCase);
    private static readonly Regex RegexStartH = new(@"(?<!\w)h", RegexOptions.IgnoreCase);
    private static readonly Regex RegexSpacePunctuation = new(@"(?<=\w\w)[!?;:](?!\w)", RegexOptions.IgnoreCase);

    public override string Accentuate(Entity<FrenchAccentComponent>? entity, string message)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "french");

        // replaces h with ' at the start of words.
        msg = RegexStartH.Replace(msg, "'");

        // spaces out ! ? : and ;.
        msg = RegexSpacePunctuation.Replace(msg, " $&");

        // replaces th with 'z or 's depending on the case
        foreach (Match match in RegexTh.Matches(msg))
        {
            var uppercase = msg.Substring(match.Index, 2).Contains("TH");
            var Z = uppercase ? "Z" : "z";
            var S = uppercase ? "S" : "s";
            var idxLetter = match.Index + 2;

            // If th is alone, just do 'z
            if (msg.Length <= idxLetter) {
                msg = msg.Substring(0, match.Index) + "'" + Z;
            } else {
                var c = "aeiouy".Contains(msg.Substring(idxLetter, 1).ToLower()) ? Z : S;
                msg = msg.Substring(0, match.Index) + "'" + c + msg.Substring(idxLetter);
            }
        }

        return msg;
    }
}
