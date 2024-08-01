using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class ItalianAccentSystem : EntitySystem
{
    private static readonly Regex RegexFirstWord = new(@"^(\S+)");

    private static readonly Regex RegexMeat = new(@"(?<=\s|^)meat(?=\s|$)", RegexOptions.IgnoreCase);

    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItalianAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message)
    {
        // Order:
        // Do text manipulations first
        // Then prefix/suffix funnyies

        var msg = message;

        // Half the time meat should be "pepperoni" instead of "prosciutto"
        foreach (Match match in RegexMeat.Matches(msg))
        {
            if (_rng.Prob(0.5f))
            {
                msg = RegexMeat.Replace(msg, "pepperoni");
            }
        }

        // direct word replacements
        msg = _replacement.ApplyReplacements(message, "italian");

        // Prefix
        if (_rng.Prob(0.02f))
        {
            var pick = _rng.Next(1, 4);
            var prefix = Loc.GetString($"accent-italian-prefix-{pick}");
            msg = prefix + " " + msg;
        }

        return msg;
    }

    private void OnAccentGet(EntityUid uid, ItalianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
