using System.Text.RegularExpressions;
using Content.Shared.Chat;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class AccentSystem : EntitySystem
{
    public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?‽])(?![\.!\?‽])", RegexOptions.Compiled);

    [SubscribeLocalEvent]
    private void AccentHandler(TransformSpeechEvent args)
    {
        if (args.Cancelled)
            return;

        var accentEvent = new AccentGetEvent(args.Sender, args.Message);

        RaiseLocalEvent(args.Sender, ref accentEvent);
        args.Message = accentEvent.Message;
    }
}
