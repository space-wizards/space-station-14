using System.Text.RegularExpressions;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SkeletonAccentSystem : RelayAccentSystem<SkeletonAccentComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly Regex BoneRegex = new(@"(?<!\w)[^aeiou]one", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override string Accentuate(string message, Entity<SkeletonAccentComponent>? ent = null)
    {
        var random = ent.HasValue
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
            : _random;

        // Order:
        // Do character manipulations first
        // Then direct word/phrase replacements
        // Then prefix/suffix

        var msg = message;

        // Character manipulations:
        // At the start of words, any non-vowel + "one" becomes "bone", e.g. tone -> bone ; lonely -> bonely; clone -> clone (remains unchanged).
        msg = BoneRegex.Replace(msg, "bone");

        // apply word replacements
        msg = _replacement.ApplyReplacements(msg, "skeleton", ent?.Owner);

        // Suffix:
        if (random.Prob(ent.HasValue ? ent.Value.Comp.AckChance : 0.3f))
            msg += " " + Loc.GetString("skeleton-suffix"); // e.g. "We only want to socialize. ACK ACK!"

        return msg;
    }
}
