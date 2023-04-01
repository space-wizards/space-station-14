using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class PirateAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PirateAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    public string Accentuate(string message, PirateAccentComponent component)
    {
        var msg = message;

        foreach (var (first, replace) in component.DirectReplacements)
        {
            msg = Regex.Replace(msg, $@"(?<!\w){Loc.GetString(first)}(?!\w)",
                Loc.GetString(replace), RegexOptions.IgnoreCase);
        }

        if (!_random.Prob(component.YarrChance))
            return msg;

        var pick = _random.Pick(component.PirateWords);
        // Reverse sanitize capital
        msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
        msg = Loc.GetString(pick) + " " + msg;

        return msg;
    }

    private void OnAccentGet(EntityUid uid, PirateAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
