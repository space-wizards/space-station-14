using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class RoarAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoarAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, RoarAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // roarrr
        message = Regex.Replace(message, "r+", "rrr");
        // roarRR
        message = Regex.Replace(message, "R+", "RRR");

        // ADT-Localization-Start
        // р => ррр
        message = Regex.Replace(
            message,
            "р+",
            _random.Pick(new List<string>() { "рр", "ррр" })
        );
        // ADT-Localization-End
        args.Message = message;
    }
}
