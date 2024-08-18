using Content.Shared.Speech.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Random;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class VulgarAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
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
                messageWords[i] = component.SwearWords[_random.Next(component.SwearWords.Length)];
            }
        }

        return string.Join(" ", messageWords);
    }

    public void OnAccentGet(EntityUid uid, VulgarAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
