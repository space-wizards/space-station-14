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

    private string[] _swearWords =
    {
        "FUCK",
        "SHIT",
        "COCK",
        "DICK",
        "FUCK YOU",
        "BITCH",
        "ASSHOLE",
        "MOTHERFUCKER",
        "BONERCOCK"
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulgarAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, AccentGetEvent args)
    {
        string[] messageWords = message.Split(" ");

        for (int i = 0; i < messageWords.Length; i++)
        {

            //Every word has a 33% chance to be replaced by a random swear word.
            if (_random.Prob(0.33f))
            {
                messageWords[i] = _swearWords[_random.Next(_swearWords.Length)];
            }
        }

        return string.Join(" ", messageWords);
    }

    public void OnAccentGet(EntityUid uid, VulgarAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, args);
    }
}
