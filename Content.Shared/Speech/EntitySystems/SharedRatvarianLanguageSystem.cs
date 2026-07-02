using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.EntitySystems;

public abstract partial class SharedRatvarianLanguageSystem : RelayAccentSystem<RatvarianLanguageComponent>
{
    public static readonly EntProtoId Ratvarian = "StatusEffectRatvarianLanguage";

    [Dependency] protected StatusEffectsSystem Status = default!;

    protected override Type[] AccentBefore => [typeof(SharedSlurredSystem), typeof(SharedStutteringSystem)];
    protected override Type[] RelayAccentBefore => [typeof(SharedSlurredSystem), typeof(SharedStutteringSystem)];

    // This is the word of Ratvar and those who speak it shall abide by His rules:
    /*
     * Any time the word "of" occurs, it's linked to the previous word by a hyphen: "I am-of Ratvar"
     * Any time "th", followed by any two letters occurs, you add a grave (`) between those two letters: "Thi`s"
     * In the same vein, any time "ti" followed by one letter occurs, you add a grave (`) between "i" and the letter: "Ti`me"
     * Wherever "te" or "et" appear and there is another letter next to the "e", add a hyphen between "e" and the letter: "M-etal/Greate-r"
     * Where "gua" appears, add a hyphen between "gu" and "a": "Gu-ard"
     * Where the word "and" appears it's linked to all surrounding words by hyphens: "Sword-and-shield"
     * Where the word "to" appears, it's linked to the following word by a hyphen: "to-use"
     * Where the word "my" appears, it's linked to the following word by a hyphen: "my-light"
     * Any Ratvarian proper noun is not translated: Ratvar, Nezbere, Sevtug, Nzcrentr and Inath-neq
        * This only applies if they're being used as a proper noun: armorer/Nezbere
    */

    private static readonly Regex ThPattern = new(@"th\w\B", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EtPattern = new(@"\Bet", RegexOptions.Compiled);
    private static readonly Regex TePattern = new(@"te\B", RegexOptions.Compiled);
    private static readonly Regex OfPattern = new(@"(\s)(of)");
    private static readonly Regex TiPattern = new(@"ti\B", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex GuaPattern = new("(gu)(a)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AndPattern = new(@"\b(\s)(and)(\s)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TomyPattern = new(@"(to|my)\s", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ProperNouns = new("(ratvar)|(nezbere)|(sevtuq)|(nzcrentr)|(inath-neq)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Applies or refreshes the Ratvarian speech status effect on an entity.
    /// </summary>
    public virtual void DoRatvarian(EntityUid uid, TimeSpan time, bool refresh)
    {
    }

    public override string Accentuate(string message, Entity<RatvarianLanguageComponent>? _)
    {
        var ruleTranslation = message;
        var finalMessage = new StringBuilder();
        var newWord = new StringBuilder();

        ruleTranslation = ThPattern.Replace(ruleTranslation, "$&`");
        ruleTranslation = EtPattern.Replace(ruleTranslation, "-$&");
        ruleTranslation = TePattern.Replace(ruleTranslation, "$&-");
        ruleTranslation = OfPattern.Replace(ruleTranslation, "-$2");
        ruleTranslation = TiPattern.Replace(ruleTranslation, "$&`");
        ruleTranslation = GuaPattern.Replace(ruleTranslation, "$1-$2");
        ruleTranslation = AndPattern.Replace(ruleTranslation, "-$2-");
        ruleTranslation = TomyPattern.Replace(ruleTranslation, "$1-");

        foreach (var word in ruleTranslation.Split(' '))
        {
            newWord.Clear();

            if (ProperNouns.IsMatch(word))
            {
                newWord.Append(word);
            }
            else
            {
                foreach (var letter in word)
                {
                    if (letter >= 'a' && letter <= 'z')
                    {
                        var letterRot = letter + 13;

                        if (letterRot > 'z')
                            letterRot -= 26;

                        newWord.Append((char)letterRot);
                    }
                    else if (letter >= 'A' && letter <= 'Z')
                    {
                        var letterRot = letter + 13;

                        if (letterRot > 'Z')
                            letterRot -= 26;

                        newWord.Append((char)letterRot);
                    }
                    else
                    {
                        newWord.Append(letter);
                    }
                }
            }

            finalMessage.Append(newWord).Append(' ');
        }

        return finalMessage.ToString().Trim();
    }
}
