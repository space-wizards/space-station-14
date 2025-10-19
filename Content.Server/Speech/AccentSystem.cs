using System.Text.RegularExpressions;
using Content.Shared.Chat;
using Content.Shared.Speech;

namespace Content.Server.Speech;

public sealed class AccentSystem : EntitySystem
{
    public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?‽])(?![\.!\?‽])", RegexOptions.Compiled);

    public override void Initialize()
    {
        SubscribeLocalEvent<TransformSpeechEvent>(AccentHandler);
    }

    private void AccentHandler(TransformSpeechEvent args)
    {
        var accentEvent = new AccentGetEvent(args.Sender, args.Message);

        RaiseLocalEvent(args.Sender, accentEvent, true);
        args.Message = accentEvent.Message;
    }
}
