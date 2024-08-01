using System.Text;
using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class GermanAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexTh = new(@"th", RegexOptions.IgnoreCase);
    private static readonly Regex RegexThe = new(@"(?<=\s|^)the(?=\s|$)", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        SubscribeLocalEvent<GermanAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        var msg = message;

        // rarely, "the" should become "das" instead of "ze"
        foreach (Match match in RegexThe.Matches(msg))
        {
            if (_rng.Prob(0.3f))
            {
                // just shift T, H and E over to D, A and S to preserve capitalization.
                msg = msg.Substring(0, match.Index) +
                      (char) (msg[match.Index] - 16) +
                      (char) (msg[match.Index + 1] - 7) +
                      (char) (msg[match.Index + 2] + 14) +
                      msg.Substring(match.Index + 3);
            }
        }

        // now, apply word replacements
        msg = _replacement.ApplyReplacements(msg, "german");

        // replaces th with zh (for zhis, zhat, etc. the => ze is handled by replacements already)
        msg = RegexTh.Replace(msg, "zh");

        // Random Umlaut Time!
        var msgBuilder = new StringBuilder(msg);
        for (var i = 0; i < msgBuilder.Length; i++)
        {
            if (_rng.Prob(0.2f)) // 20% of all eligible vowels become umlauts
            {
                msgBuilder[i] = msgBuilder[i] switch
                {
                    'A' => 'Ä',
                    'a' => 'ä',
                    'O' => 'Ö',
                    'o' => 'ö',
                    'U' => 'Ü',
                    'u' => 'ü',
                    _ => msgBuilder[i]
                };
            }
        }

        return msgBuilder.ToString();
    }

    private void OnAccent(EntityUid uid, GermanAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
