using System.Text.RegularExpressions;
using Content.Server._Starlight.Speech.Components;
using Content.Server.Speech;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Speech.EntitySystems;

public sealed class GrowlingAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrowlingAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, GrowlingAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // r => rrr
        message = Regex.Replace(
            message,
            "r+",
            _random.Pick(new List<string> { "rr", "rrr" })
        );
        // R => RRR
        message = Regex.Replace(
            message,
            "R+",
            _random.Pick(new List<string> { "RR", "RRR" })
        );

        args.Message = message;
    }
}
