using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;

namespace Content.Server.Speech.EntitySystems;

public sealed class RatvarianLanguageSystem : SharedRatvarianLanguageSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    private const string RatvarianKey = "RatvarianLanguage";
    //@"(?!ratvar\b)\b[a-zA-Z]+"
    private static Regex Ratvarian = new Regex(@"(?!ratvar\b)\b[a-zA-Z]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        var originalMessage = message;
        char newLetter = ' ';
        var finalMessage = new StringBuilder();

        var nonExcludedWords = Ratvarian.Matches(originalMessage);

        foreach (Match match in nonExcludedWords)
        {
        }

        //TODO: iterate through each word then iterate through each letter
        for (int i = 0; i < message.Length; i++)
        {
            //TODO: This didn't work
            if (Ratvarian.IsMatch(originalMessage))
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

            /*if (Ratvarian.IsMatch(originalMessage[i].ToString()))
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
            }*/

            else if (originalMessage[i] == ' ')
                newLetter = ' ';

            finalMessage.Append(newLetter);
        }
        return finalMessage.ToString();
    }
}
