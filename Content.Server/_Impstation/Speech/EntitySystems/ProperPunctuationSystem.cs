using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class ProperPunctuationSystem : EntitySystem
{
    // @formatter:off
    private static readonly Regex RegexEndsWithAnyPunctuation = new(@"[,:;!?\.-]+$");
    // @formatter:on

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProperPunctuationComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, ProperPunctuationComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // If the message doesn't end with any punctuation, we add a period
        if (!RegexEndsWithAnyPunctuation.IsMatch(message))
        {
            message += ".";
        }

        args.Message = message;

    }
}
