using System.Text.RegularExpressions;
using Content.Server._Tinystation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._Tinystation.Speech.EntitySystems;

public sealed partial class KnightAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    [GeneratedRegex(@"\bmy\s+(?=[aeiouAEIOU])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MyBeforeVowelRegex();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnightAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, KnightAccentComponent component)
    {
        var msg = message;

        msg = MyBeforeVowelRegex().Replace(msg, "mine ");

        msg = _replacement.ApplyReplacements(msg, "knight");

        if (_random.Prob(component.ackChance))
        {
            msg += " " + Loc.GetString("knight-suffix");
        }
        return msg;
    }

    private void OnAccentGet(EntityUid uid, KnightAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
