using Content.Server.Speech.Components;
using System.Text.RegularExpressions;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// System that gives the speaker a faux-French accent.
/// </summary>
public sealed class FrenchAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexTh = new(@"th", RegexOptions.IgnoreCase);
    private static readonly Regex RegexStartH = new(@"(?<!\w)h", RegexOptions.IgnoreCase);
    private static readonly Regex RegexSpacePunctuation = new(@"(?<=\w\w)[!?;:](?!\w)", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FrenchAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, FrenchAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "french");

        // replaces h with ' at the start of words.
        msg = RegexStartH.Replace(msg, "'");

        // spaces out ! ? : and ;.
        msg = RegexSpacePunctuation.Replace(msg, " $&");
        
        // replaces th with 'z or 's depending on the case
        var offset = 0;
        foreach (Match match in RegexTh.Matches(msg))
        {
            var i = match.Index + offset;

            var uppercase = msg.Substring(i, 2).Contains("TH");
            var Z = uppercase ? "Z" : "z";
            var S = uppercase ? "S" : "s";
            var idxLetter = i + 2;

            // If th is alone, just do 'zis
            if (msg.Length <= idxLetter) {
                msg = msg.Substring(0, i) + "'" + Z;
            } else {
                var c = "aeiouy".Contains(msg.Substring(idxLetter, 1).ToLower()) ? Z : S;

                // french people tend to force 'ze s when talking, especially loudly
                if (c == S && _random.Prob(uppercase ? 0.75f : 0.25f)) {
                    c += c;
                    offset += 1;
                }

                msg = msg.Substring(0, i) + "'" + c + msg.Substring(idxLetter);
            }
        }

        return msg;
    }

    private void OnAccentGet(EntityUid uid, FrenchAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
