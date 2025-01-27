using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class SharpInflectionSystem : EntitySystem
{
    // @formatter:off
    private static readonly Regex RegexEndsWithExclamation = new(@"[!]+$");
    private static readonly Regex RegexEndsWithQuestion = new(@"[?]+$");
    private static readonly Regex RegexEndsWithPeriod = new(@"[\.]+$");
    private static readonly Regex RegexEndsWithAnyPunctuation = new(@"[!?\.]+$");
    // @formatter:on

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharpInflectionComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SharpInflectionComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = RegexEndsWithExclamation.Replace(message, "!!");
        message = RegexEndsWithQuestion.Replace(message, "?!!");
        message = RegexEndsWithPeriod.Replace(message, "...");

        // If the message doesn't end with any punctuation, we add ... anyway
        if (!RegexEndsWithAnyPunctuation.IsMatch(message))
        {
            message += "...";
        }

        args.Message = message;
    }
}
