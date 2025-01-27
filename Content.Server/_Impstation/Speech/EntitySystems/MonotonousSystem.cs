using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class MonotonousSystem : EntitySystem
{
    // @formatter:off
    private static readonly Regex RegexAnyPunctuationNotPeriod = new(@"[!?]+");
    // @formatter:on

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MonotonousComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, MonotonousComponent component, AccentGetEvent args)
    {
        args.Message = RegexAnyPunctuationNotPeriod.Replace(args.Message, ".");

        // If the message doesn't end with a letter or punctuation, we add a period for sharpness
        if (!char.IsLetterOrDigit(args.Message[^1]) && !char.IsPunctuation(args.Message[^1]))
        {
            args.Message += ".";
        }
    }
}
