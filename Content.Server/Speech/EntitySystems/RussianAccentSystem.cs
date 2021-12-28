using System.Text;
using Content.Server.Speech.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class RussianAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RussianAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        var accentedMessage = new StringBuilder(message);

        accentedMessage.Replace('b', 'в');
        accentedMessage.Replace('N', 'И');
        accentedMessage.Replace('n', 'и');
        accentedMessage.Replace('K', 'К');
        accentedMessage.Replace('k', 'к');
        accentedMessage.Replace('m', 'м');
        accentedMessage.Replace('h', 'н');
        accentedMessage.Replace('t', 'т');
        accentedMessage.Replace('R', 'Я');
        accentedMessage.Replace('r', 'я');
        accentedMessage.Replace('Y', 'У');
        accentedMessage.Replace('W', 'Ш');
        accentedMessage.Replace('w', 'ш');

        return accentedMessage.ToString();
    }

    private void OnAccent(EntityUid uid, RussianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
