using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;

namespace Content.Shared.Speech.Accents;

public sealed class GermanAccent : IAccent
{
    public string Name { get; } = "German";

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexTh = new(@"(?<=\s|^)th", RegexOptions.IgnoreCase);
    private static readonly Regex RegexThe = new(@"(?<=\s|^)the(?=\s|$)", RegexOptions.IgnoreCase);

    public string Accentuate(string message, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        var msg = message;

        // rarely, "the" should become "das" instead of "ze"
        // TODO: The ReplacementAccentSystem should have random replacements this built-in.
        foreach (Match match in RegexThe.Matches(msg))
        {
            if (_random.Prob(0.3f))
            {
                // just shift T, H and E over to D, A and S to preserve capitalization
                msg = msg.Substring(0, match.Index) +
                      (char)(msg[match.Index] - 16) +
                      (char)(msg[match.Index + 1] - 7) +
                      (char)(msg[match.Index + 2] + 14) +
                      msg.Substring(match.Index + 3);
            }
        }

        // now, apply word replacements
        msg = _replacement.ApplyReplacements(msg, "german");

        // replace th with zh (for zhis, zhat, etc. the => ze is handled by replacements already)
        var msgBuilder = new StringBuilder(msg);
        foreach (Match match in RegexTh.Matches(msg))
        {
            // just shift the T over to a Z to preserve capitalization
            msgBuilder[match.Index] = (char) (msgBuilder[match.Index] + 6);
        }

        // Random Umlaut Time! (The joke outweighs the emotional damage this inflicts on actual Germans)
        var umlautCooldown = 0;
        for (var i = 0; i < msgBuilder.Length; i++)
        {
            if (umlautCooldown == 0)
            {
                if (_random.Prob(0.1f)) // 10% of all eligible vowels become umlauts)
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
                    umlautCooldown = 4;
                }
            }
            else
            {
                umlautCooldown--;
            }
        }

        return msgBuilder.ToString();
    }
}
