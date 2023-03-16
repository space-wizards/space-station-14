using Content.Server.Speech.Components;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
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
    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { Loc.GetString($"accent-pirate-word-1"), Loc.GetString($"accent-pirate-word-2") },
        { Loc.GetString($"accent-pirate-word-3"), Loc.GetString($"accent-pirate-word-4") },
        { Loc.GetString($"accent-pirate-word-5"), Loc.GetString($"accent-pirate-word-6") },
        { Loc.GetString($"accent-pirate-word-7"), Loc.GetString($"accent-pirate-word-8") },
    };

    public string Accentuate(string message, PirateAccentComponent component)
    {

        var msg = message;

        foreach (var (first, replace) in DirectReplacements)
        {
            msg = Regex.Replace(msg, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }

        // Suffix:
        if (_random.Prob(component.YarChance))
        {
            var pick = _random.Next(1, 4);

            // Reverse sanitize capital
            msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            msg = Loc.GetString($"accent-pirate-prefix-{pick}") + " " + msg;
        }

        return msg;
    }
    private void OnAccentGet(EntityUid uid, PirateAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}

