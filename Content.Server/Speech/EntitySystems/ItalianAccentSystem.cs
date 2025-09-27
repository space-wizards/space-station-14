using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class ItalianAccentSystem : EntitySystem
{
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
        // Last prefix funnies

        // direct word replacements
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "italian");

        // Prefix
        if (_random.Prob(0.05f))
        {
            //Checks if the first word of the sentence is all caps
            //So the prefix can be all-capped and to not re-sanitize the capital
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
