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

        msg = Regex.Replace(msg, Loc.GetString($"{component.PirateWord}"), Loc.GetString($"{component.PirateResponse}"), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, Loc.GetString($"{component.PirateWordOne}"), Loc.GetString($"{component.PirateResponseOne}"), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, Loc.GetString($"{component.PirateWordTwo}"), Loc.GetString($"{component.PirateResponseTwo}"), RegexOptions.IgnoreCase);

        // Suffix:
        if (_random.Prob(component.YarChance))
        {
            var pick = _random.Next(1, 4);

            // Reverse sanitize capital
            msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            msg = Loc.GetString($"{component.PiratePrefix}{pick}") + " " + msg;
        }

        return msg;
    }
    private void OnAccentGet(EntityUid uid, PirateAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
