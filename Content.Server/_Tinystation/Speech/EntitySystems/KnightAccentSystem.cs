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

    [GeneratedRegex(@"(?<!\w)[^aeiou]one", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex BoneRegex();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnightAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, KnightAccentComponent component)
    {
        // Order:
        // Do character manipulations first
        // Then direct word/phrase replacements
        // Then prefix/suffix

        var msg = message;

        // Character manipulations:
        // At the start of words, any non-vowel + "one" becomes "bone", e.g. tone -> bone ; lonely -> bonely; clone -> clone (remains unchanged).
        msg = BoneRegex().Replace(msg, "bone");

        // apply word replacements
        msg = _replacement.ApplyReplacements(msg, "knight");

        // Suffix:
        if (_random.Prob(component.ackChance))
        {
            msg += (" " + Loc.GetString("knight-suffix"));
        }
        return msg;
    }

    private void OnAccentGet(EntityUid uid, KnightAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
