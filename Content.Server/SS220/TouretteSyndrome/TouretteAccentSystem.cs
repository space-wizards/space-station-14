using Content.Server.Speech.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using FastAccessors;

namespace Content.Server.Speech.EntitySystems;

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
public sealed class TouretteAccentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TouretteAccentComponent, AccentGetEvent>(OnAccentGet);
        SubscribeLocalEvent<TouretteAccentComponent, ComponentStartup>(OnTouretteStartup);
    }


    private void OnTouretteStartup(EntityUid uid, TouretteAccentComponent component, ComponentStartup args)
    {
        component.SwearChance = _random.NextFloat(0f, 0.7f);

        if (component.SwearChance >= 0.6f)
            component.SwearChance = 0f;

        var en = _proto.EnumeratePrototypes<TouretteCollectionPrototype>();
        component.TouretteWords = en.ToArray()[_random.Next(0, en.Count() - 1)].Replics;
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    private string Accentuate(string message, TouretteAccentComponent component)
    {
        var msg = message;

        if (!_random.Prob(component.SwearChance))
            return msg;

        if (string.IsNullOrEmpty(msg))
            return msg;

        var pick = _random.Pick(component.TouretteWords);
        msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
        msg = Loc.GetString(pick) + ", " + msg;

        return msg;
    }

    private void OnAccentGet(EntityUid uid, TouretteAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
