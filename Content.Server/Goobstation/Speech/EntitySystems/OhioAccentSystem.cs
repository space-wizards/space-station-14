using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class OhioAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OhioAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, OhioAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "ohio");

        // Prefix
        if (_random.Prob(0.15f))
        {
            var pick = _random.Next(1, 3);

            // Reverse sanitize capital
            message = message[0].ToString().ToLower() + message.Remove(0, 1);
            message = Loc.GetString($"accent-ohio-prefix-{pick}") + " " + message;
        }

        // Sanitize capital again, in case we substituted a word that should be capitalized
        message = message[0].ToString().ToUpper() + message.Remove(0, 1);

        // Suffixes
        if (_random.Prob(0.3f))
        {
            var pick = _random.Next(1, 8);
            message += Loc.GetString($"accent-ohio-suffix-{pick}");
        }

        args.Message = message;
    }
};
