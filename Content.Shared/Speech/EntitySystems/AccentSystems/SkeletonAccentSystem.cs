using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed partial class SkeletonAccentSystem : AccentSystem<SkeletonAccentComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex BoneRegex = new(@"(?<!\w)[^aeiou]one");

    public override string Accentuate(Entity<SkeletonAccentComponent>? entity, string message)
    {
        // Order:
        // Do character manipulations first
        // Then direct word/phrase replacements
        // Then prefix/suffix

        var msg = message;

        // Character manipulations:
        // At the start of words, any non-vowel + "one" becomes "bone", e.g. tone -> bone ; lonely -> bonely; clone -> clone (remains unchanged).
        msg = BoneRegex.Replace(msg, "bone");

        // apply word replacements
        msg = _replacement.ApplyReplacements(msg, "skeleton");

        // Suffix:
        if (entity == null || _random.Prob(entity.Value.Comp.AckChance))
        {
            msg += (" " + Loc.GetString("skeleton-suffix")); // e.g. "We only want to socialize. ACK ACK!"
        }
        return msg;
    }
}
