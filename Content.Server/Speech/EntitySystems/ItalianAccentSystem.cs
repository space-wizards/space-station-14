using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class ItalianAccentSystem : EntitySystem
{
    private static readonly Regex RegexProsciutto = new(@"(?<=\s|^)prosciutto(?=\s|$)", RegexOptions.IgnoreCase);

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
            var pick = _random.Next(1, 5);
            msg = Loc.GetString($"accent-italian-prefix-{pick}") + " " + msg;
        }

        return msg;
    }

    private void OnAccentGet(EntityUid uid, ItalianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
