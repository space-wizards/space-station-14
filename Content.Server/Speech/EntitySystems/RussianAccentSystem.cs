using System.Text;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class RussianAccentSystem : EntitySystem
{
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
				
				'в' => 'b',
                'И' => 'N',
                'и' => 'n',
                'К' => 'K',
                'к' => 'k',
                'м' => 'm',
                'н' => 'h',
                'т' => 't',
                'Я' => 'R',
                'я' => 'r',
                'У' => 'Y',
                'Ш' => 'W',
                'ш' => 'w',
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
