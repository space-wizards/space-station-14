using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class SkeletonAccent : IAccent
{
    public string Name { get; } = "Skeleton";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    private static readonly Regex BoneRegex = new(@"(?<!\w)[^aeiou]one");

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();
        var ackChance = 0f;
        if (attributes.TryGetValue("ackChance", out var parameter))
            ackChance = parameter.LongValue!.Value;

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
        if (_random.Prob(ackChance))
        {
            msg += (" " + Loc.GetString("skeleton-suffix")); // e.g. "We only want to socialize. ACK ACK!"
        }
        return msg;
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        if (c is SkeletonAccentComponent comp)
        {
            ev.Accents.Add(
                Name,
                new Dictionary<string, MarkupParameter>() { { "ackChance", new MarkupParameter((long)comp.AckChance) } });
        }
    }
}
