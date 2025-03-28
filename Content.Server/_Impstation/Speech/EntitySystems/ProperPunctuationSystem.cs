using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class ProperPunctuationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProperPunctuationComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, ProperPunctuationComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        if (string.IsNullOrWhiteSpace(message))
            return;

        // If the message doesn't end with any punctuation, we add a period
        if (!char.IsPunctuation(message[^1]))
            message += ".";

        args.Message = message;
    }
}
