using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class ItalianAccentSystem : EntitySystem
{
    private static readonly Regex RegexProsciutto = new(@"(?<=\s|^)prosciutto(?=\s|$)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexFirstWord = new(@"^(\S+)");


    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItalianAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message)
    {
        // Order:
        // Do text manipulations
        // Then alternative meat
        // Last prefix funnyies

        // direct word replacements
        var msg = _replacement.ApplyReplacements(message, "italian");

        // Half the time meat should be "pepperoni" instead of "prosciutto"
        foreach (Match match in RegexProsciutto.Matches(msg))
        {
            if (_random.Prob(0.5f))
            {
                msg = msg.Remove(match.Index, match.Length).Insert(match.Index, "pepperoni");
            }
        }

        // Prefix
        if (_random.Prob(0.05f))
        {
            //Checks if the first word of the sentence is all caps
            //So the prefix can be allcapped and to not resanitize the captial
            var firstWordAllCaps = !RegexFirstWord.Match(msg).Value.Any(char.IsLower);
            var pick = _random.Next(1, 5);

            // Reverse sanitize capital
            var prefix = Loc.GetString($"accent-italian-prefix-{pick}");
            if (!firstWordAllCaps)
                msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            else
                prefix = prefix.ToUpper();
            msg = prefix + " " + msg;
        }

        // Sanitize capital again, in case we substituted a word that should be capitalized
        msg = msg[0].ToString().ToUpper() + msg.Remove(0, 1);


        return msg;
    }

    private void OnAccentGet(EntityUid uid, ItalianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
