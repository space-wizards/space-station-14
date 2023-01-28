using System.Globalization;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class MobsterAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        // Corvax-Localization-Start
        { "утащил", "сдёрнул" },
        { "принеси", "надыбай" },
        { "принесите", "надыбайте" },
        { "сб", "мусора" },
        { "враг", "шелупонь" },
        { "враги", "шелупонь" },
        { "тревога", "шухер" },
        { "заметили", "спалили" },
        { "оружие", "валына" },
        { "убийство", "мокруха" },
        { "убить", "замочить" },
        { "убей", "вальни" },
        { "убейте", "завалите" },
        { "еда", "жратва"},
        { "еды", "жратвы"},
        { "убили", "замаслили" },
        { "ранен", "словил маслину"},
        { "мертв", "спит с рыбами"},
        { "мёртв", "спит с рыбами"},
        { "мертва", "спит с рыбами"},
        { "хэй", "йоу" },
        { "хей", "йоу" },
        { "здесь", "здеся" },
        { "тут", "тута" },
        { "привет", "аве" },
        { "плохо", "ацтой" },
        { "хорошо", "агонь" },
        // Corvax-Localization-End
        { "let me", "lemme" },
        { "should", "oughta" },
        { "the", "da" },
        { "them", "dem" },
        { "attack", "whack" },
        { "kill", "whack" },
        { "murder", "whack" },
        { "dead", "sleepin' with da fishies"},
        { "hey", "ey'o" },
        { "hi", "ey'o"},
        { "hello", "ey'o"},
        { "rules", "roolz" },
        { "you", "yous" },
        { "have to", "gotta" },
        { "going to", "boutta" },
        { "about to", "boutta" },
        { "here", "'ere" }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobsterAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, MobsterAccentComponent component)
    {
        // Order:
        // Do text manipulations first
        // Then prefix/suffix funnyies

        var msg = message;

        foreach (var (first, replace) in DirectReplacements)
        {
            msg = Regex.Replace(msg, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }

        // thinking -> thinkin'
        // king -> king
        msg = Regex.Replace(msg, @"(?<=\w\w)ing(?!\w)", "in'", RegexOptions.IgnoreCase);

        // or -> uh and ar -> ah in the middle of words (fuhget, tahget)
        msg = Regex.Replace(msg, @"(?<=\w)or(?=\w)", "uh", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<=\w)ar(?=\w)", "ah", RegexOptions.IgnoreCase);

        // Prefix
        if (_random.Prob(0.15f))
        {
            var pick = _random.Next(1, 2);

            // Reverse sanitize capital
            msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            msg = Loc.GetString($"accent-mobster-prefix-{pick}") + " " + msg;
        }

        // Sanitize capital again, in case we substituted a word that should be capitalized
        msg = msg[0].ToString().ToUpper() + msg.Remove(0, 1);

        // Suffixes
        if (_random.Prob(0.4f))
        {
            if (component.IsBoss)
            {
                var pick = _random.Next(1, 4);
                msg += Loc.GetString($"accent-mobster-suffix-boss-{pick}");
            }
            else
            {
                var pick = _random.Next(1, 3);
                msg += Loc.GetString($"accent-mobster-suffix-minion-{pick}");
            }
        }

        return msg;
    }

    private void OnAccentGet(EntityUid uid, MobsterAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
