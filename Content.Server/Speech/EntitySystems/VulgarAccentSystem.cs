using Content.Shared.Speech.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Text;

namespace Content.Server.Speech.EntitySystems;

public sealed class VulgarAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulgarAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, VulgarAccentComponent component)
    {
        string[] messageWords = message.Split(" ");

        for (int i = 0; i < messageWords.Length; i++)
        {
            //Every word has a percentage chance to be replaced by a random swear word from the component's array.
            if (_random.Prob(component.SwearProb))
            {
                if (!_prototypeManager.TryIndex(component.Pack, out var messagePack))
                    return message;


                string swearWord = _loc.GetString(_random.Pick(messagePack.Values));
                messageWords[i] = swearWord;
            }
        }

        return string.Join(" ", messageWords);
    }

    public void OnAccentGet(EntityUid uid, VulgarAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
