using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;

namespace Content.Server.Speech.EntitySystems;

public sealed class RatvarianLanguageSystem : SharedRatvarianLanguageSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    //TODO: Need to add the other ratvarian language rules

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

    //TODO: Make a class of or put the Regex options into the component
    private const string RatvarianKey = "RatvarianLanguage";
    //@"(?!ratvar\b)\b[a-zA-Z]+"
    private static Regex Ratvarian = new Regex(@"(?!ratvar\b)\b[a-zA-Z]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static Regex THPattern = new Regex(@"th\w\B", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static Regex TEPattern = new Regex(@"\Bet", RegexOptions.Compiled);
    private static Regex ETPattern = new Regex(@"te\B",RegexOptions.Compiled);
    private static Regex OFPattern = new Regex(@"(\s)(of)");
    private static Regex TIPattern = new Regex(@"ti\B", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static Regex GUAPattern = new Regex(@"(gu)(a)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static Regex ANDPattern = new Regex(@"\b(\s)(and)(\s)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static Regex TOMYPattern = new Regex(@"(to|my)\s", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly HashSet<Regex> RatvarianTranslationRules = new() {THPattern, TEPattern, ETPattern, OFPattern, TIPattern, GUAPattern, ANDPattern, TOMYPattern};

    // see if you can run the replacements in the regex
    // regex [th] th\w\B IgnoreCase, ($&` Replace) OR (th\w)(\w) ($1`$2)
    // regex [te] \Bet -$&
    // regex [et] te\B  $&-
    // regex [of] (\s)(of) -$2 Replace
    // regex [ti] ti\B Ignore Case ($&` Replace)
    // regex [gua] (gu)(a) ignore case $1-$2
    // regex [and] \b(\s)(and)(\s) ignore case -$2-
    // regex [to] [my] (to|my)\s ignore case $1-

    // Message (input) > Translation (output)

    //1 - Take in message:
    //"This timid metal granted of Ratvar's armorer shall guard and guide me to see my victory"
    //2 - Run the pre translation checks (ratvarian rules) (looping or regex)
    //  regex: tbd
    //  looping: split by space or take in the entire sentence (won't work by character)
    //3 - Rules implemented:
    //"Thi`s ti`mid m-etal grante-d-of Ratvar's armorer shall gu-ard-and-guide me to-see my-victory"
    //4 - block out proper nouns: Ratvar's
    //Run translation and output:
    //"Guv`f gv`zvq z-rgny tenagr-q-bs Ratvar's nezbere funyy th-neq-naq-thvqr zr gb-frr zl-ivpgbel"

    //Notes: you may need to take in stutters

    public override void Initialize()
    {
        SubscribeLocalEvent<RatvarianLanguageComponent, AccentGetEvent>(OnAccent);
    }

    public override void DoRatvarian(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        _statusEffects.TryAddStatusEffect<RatvarianLanguageComponent>(uid, RatvarianKey, time, refresh, status);
    }

    private void OnAccent(EntityUid uid, RatvarianLanguageComponent component, AccentGetEvent args)
    {
        args.Message = Translate(args.Message);
    }

    private string Translate(string message)
    {
        //TODO: split by space
        var finalMessage = message;
        //var finalMessage = new StringBuilder();

        finalMessage = THPattern.Replace(finalMessage, "$&`");
        finalMessage = TEPattern.Replace(finalMessage, "-$&");
        finalMessage = ETPattern.Replace(finalMessage, "$&-");
        finalMessage = OFPattern.Replace(finalMessage, "-$2");
        finalMessage = TIPattern.Replace(finalMessage, "$&`");
        finalMessage = GUAPattern.Replace(finalMessage, "$1-$2");
        finalMessage = ANDPattern.Replace(finalMessage, "-$2-");
        finalMessage = TOMYPattern.Replace(finalMessage, "$1-");

        return finalMessage;

        //return THPattern.Replace(message, "$&`");

        //THPattern.Replace(message, "$&`");

        //var nonExcludedWords = Ratvarian.Matches(originalMessage);

        //TODO: iterate through each word then iterate through each character
        //  Needed because of different hyphening and rules
        /*for (int i = 0; i < message.Length; i++)
        {
            //TODO: This didn't work
            if (Ratvarian.IsMatch(originalMessage))
            {
                var letter = originalMessage[i];

                //TODO: Replace with something other than the magic number
                // Letters are much more readable than numbers
                // Easier to RNG predict too? Still good to know what each char is number wise
                if (letter >= 97 && letter <= 122)
                {
                    var letterRot = letter + 13;

                    if (letterRot > 122)
                        letterRot -= 26;

                    newLetter = (char)letterRot;
                }
                else if (letter >= 65 && letter <= 90)
                {
                    var letterRot = letter + 13;

                    if (letterRot > 90)
                        letterRot -= 26;

                    newLetter = (char)letterRot;
                }
                else
                {
                    newLetter = letter;
                }
            }

            if (Ratvarian.IsMatch(originalMessage[i].ToString()))
            {
                var letter = originalMessage[i];

                if (letter >= 97 && letter <= 122)
                {
                    var letterRot = letter + 13;

                    if (letterRot > 122)
                        letterRot -= 26;

                    newLetter = (char)letterRot;
                }
                else if (letter >= 65 && letter <= 90)
                {
                    var letterRot = letter + 13;

                    if (letterRot > 90)
                        letterRot -= 26;

                    newLetter = (char)letterRot;
                }
                else
                {
                    newLetter = letter;
                }
            }

            else if (originalMessage[i] == ' ')
                newLetter = ' ';

            finalMessage.Append(newLetter);
        }*/
        //return finalMessage.ToString();
    }
}
