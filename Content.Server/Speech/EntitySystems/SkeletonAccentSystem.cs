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
        { "fucked", "boned"},
        { "fuck", "RATTLE RATTLE" },
        { "fck", "RATTLE RATTLE" },
        { "shit", "RATTLE RATTLE" }, // Capitalize RATTLE RATTLE regardless of original message case.
        { "definitely", "make no bones about it" },
        { "absolutely", "make no bones about it" },
        { "afraid", "rattled"},
        { "scared", "rattled"},
        { "spooked", "rattled"},
        { "shocked", "rattled"},
        { "killed", "skeletonized"},
        { "humorous", "humerus"},
        { "to be a", "tibia"},
        { "under", "ulna"},
		
		{ "пошел ты", "Я с тобой готов пораскинуть КОСТЕЙ" },
        { "послан", "насажен"},
        { "черт", "КЛАЦ КЛАЦ" },
        { "бля", "КЛАЦ КЛАЦ" },
        { "сука", "КЛАЦ КЛАЦ" },
        { "нахуй", "КЛАЦ КЛАЦ" },
        { "хуй", "КЛАЦ КЛАЦ" },
        { "хер", "КЛАЦ КЛАЦ" },
        { "член", "КЛАЦ КЛАЦ" },
        { "лох", "КЛАЦ КЛАЦ" },
        { "пидор", "КЛАЦ КЛАЦ" },
        { "естественно", "да без задней кости" },
        { "конечно", "да без задней кости" },
        { "бояться", "коститься"},
        { "страшно", "костно"},
        { "боюсь", "костьщу"},
        { "боишься", "костишь"},
        { "мертв", "скелетонизирован"},
        { "убит", "скелетонизирован"},
        { "рука", "ру-кость"},
        { "пицца", "череп-пицца"},
        { "пугаться", "костить"}
    };

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
        msg = Regex.Replace(msg, @"(?<!\w)[^aeiou]one", "bone", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)[^aeiou]ост", "кост-", RegexOptions.IgnoreCase);

        // Direct word/phrase replacements:
        foreach (var (first, replace) in DirectReplacements)
        {
            msg = Regex.Replace(msg, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }

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
