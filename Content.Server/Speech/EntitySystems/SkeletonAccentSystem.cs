using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class SkeletonAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "fuck you", "I've got a BONE to pick with you" },
        { "fuck", "rattle rattle" },
        { "shit", "rattle rattle" },
        { "definitely", "make no bones about it" },
        { "absolutely", "make no bones about it" },
        { "lonely", "bonely"}
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkeletonAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, SkeletonAccentComponent component)
    {
        // Order:
        // Do replacements first
        // Then prefix/suffix

        var msg = message;

        foreach (var (first, replace) in DirectReplacements)
        {
            msg = Regex.Replace(msg, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }

        // Suffix
        if (_random.Prob(component.ackChance))
        {
            msg += (" " + Loc.GetString("skeleton-suffix")); // ACK ACK!
        }
        return msg;
    }

    private void OnAccentGet(EntityUid uid, SkeletonAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
