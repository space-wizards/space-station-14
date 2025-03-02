using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class ProperCapitalizationSystem : EntitySystem
{
    // @formatter:off
    private static readonly Regex RegexPunctuationThenWord = new(@"([.!?])\s+([a-z])");
    // @formatter:on

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProperCapitalizationComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, ProperCapitalizationComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // When a word follows punctuation, capitalize the first letter
        message = RegexPunctuationThenWord.Replace(message, match => match.Groups[1].Value + " " + match.Groups[2].Value.ToUpper());

        args.Message = message;

    }
}
