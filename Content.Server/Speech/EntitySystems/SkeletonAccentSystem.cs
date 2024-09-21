using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class SkeletonAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    [GeneratedRegex(@"(?<!\w)[^aeiou]one", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex BoneRegex();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkeletonAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, SkeletonAccentComponent component)
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
        msg = _replacement.ApplyReplacements(msg, "skeleton");

        // Suffix:
        if (_random.Prob(component.ackChance))
        {
            msg += (" " + Loc.GetString("skeleton-suffix")); // e.g. "We only want to socialize. ACK ACK!"
        }
        return msg;
    }

    private void OnAccentGet(EntityUid uid, SkeletonAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
