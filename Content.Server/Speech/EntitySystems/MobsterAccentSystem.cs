using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class MobsterAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "let me", "lemme" },
        { "should", "oughta" },
        { "the", "da" },
        { "them", "dem" },
        { "attack", "whack" },
        { "kill", "whack" },
        { "murder", "whack" },
        { "dead", "sleepin' with da fishies"},
        { "hey", "ey'o" },
        { "hi", "ey'o"},
        { "hello", "ey'o"},
        { "rules", "roolz" },
        { "you", "yous" },
        { "have to", "gotta" },
        { "going to", "boutta" },
        { "about to", "boutta" },
        { "here", "'ere" }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobsterAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, MobsterAccentComponent component)
    {
        // Order:
        // Do text manipulations first
        // Then prefix/suffix funnyies

        // direct word replacements
        var msg = _replacement.ApplyReplacements(message, "mobster");

        // thinking -> thinkin'
        // king -> king
        //Uses captures groups to make sure the captialization of IN is kept
        msg = Regex.Replace(msg, @"(?<=\w\w)(in)g(?!\w)", "$1'", RegexOptions.IgnoreCase);

        // or -> uh and ar -> ah in the middle of words (fuhget, tahget)
        msg = Regex.Replace(msg, @"(?<=\w)o[Rr](?=\w)", "uh");
        msg = Regex.Replace(msg, @"(?<=\w)O[Rr](?=\w)", "UH");
        msg = Regex.Replace(msg, @"(?<=\w)a[Rr](?=\w)", "ah");
        msg = Regex.Replace(msg, @"(?<=\w)A[Rr](?=\w)", "AH");

        // Prefix
        if (_random.Prob(0.15f))
        {
            //Checks if the first word of the sentence is all caps
            //So the prefix can be allcapped and to not resanitize the captial
            var firstWordAllCaps = !Regex.Match(msg, @"^(\S+)").Value.Any(char.IsLower);
            var pick = _random.Next(1, 2);

            // Reverse sanitize capital
            var prefix = Loc.GetString($"accent-mobster-prefix-{pick}");
            if (!firstWordAllCaps)
                msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            else
                prefix = prefix.ToUpper();
            msg = prefix + " " + msg;
        }

        // Sanitize capital again, in case we substituted a word that should be capitalized
        msg = msg[0].ToString().ToUpper() + msg.Remove(0, 1);

        // Suffixes
        if (_random.Prob(0.4f))
        {
            //Checks if the last word of the sentence is all caps
            //So the suffix can be allcapped
            var lastWordAllCaps = !Regex.Match(msg, @"(\S+)$").Value.Any(char.IsLower);
            var suffix = "";
            if (component.IsBoss)
            {
                var pick = _random.Next(1, 4);
                suffix = Loc.GetString($"accent-mobster-suffix-boss-{pick}");
            }
            else
            {
                var pick = _random.Next(1, 3);
                suffix = Loc.GetString($"accent-mobster-suffix-minion-{pick}");                
            }
            if (lastWordAllCaps)
                suffix = suffix.ToUpper();
            msg += suffix;
        }

        return msg;
    }

    private void OnAccentGet(EntityUid uid, MobsterAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
