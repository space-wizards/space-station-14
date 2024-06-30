using System.Text;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class RussianAccentSystem : EntitySystem
{
    private Dictionary<string, string> _replacement = new()
    {
        // base
        { "hello", "привэт" },
        { "hi", "здаров" },
        { "english", "инглиш" },
        { "yes", "да" },
        // Slang
        { "union", "Союз" },
        { "comrade", "товарищ" },
        { "vodka", "водка" },
        { "water", "водка" },
        { "i", "я" },
        { "nanotrasen", "капиталисты" },
        { "lizard", "ящер" },
        { "bear", "медвежонок" },
        { "beer", "пивко" },
        { "too far", "на Камчатке" },
        // SOME terrible words
        { "shit", "дэрьмо" },
        { "fuck", "blyat" },
        // Pronunciation
        { "r", "р" },
        { "p", "п" },
        { "e", "э" }
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>(OnAccent);
    }

    public static string Accentuate(string message)
    {
        var accentedMessage = new StringBuilder(message);

        for (var i = 0; i < accentedMessage.Length; i++)
        {
            var c = accentedMessage[i];

            accentedMessage[i] = c switch
            {
                'b' => 'в',
                'N' => 'И',
                'n' => 'и',
                'K' => 'К',
                'k' => 'к',
                'm' => 'м',
                'h' => 'н',
                't' => 'т',
                'R' => 'Я',
                'r' => 'я',
                'Y' => 'У',
                'W' => 'Ш',
                'w' => 'ш',
                _ => accentedMessage[i]
            };
        }

        return accentedMessage.ToString();
    }

    private void OnAccent(EntityUid uid, RussianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
