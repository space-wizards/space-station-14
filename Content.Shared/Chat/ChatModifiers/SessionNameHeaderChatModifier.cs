using Content.Shared.Chat.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds a [EntityNameHeader="name"] tag in front of the message.
/// The string inside the tag is based upon the sending entity, and may be changed via certain transformation events (e.g. voicemasks).
/// Once this tag is processed, it gets replaced with the string inside the tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SessionNameHeaderChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        if (!chatMessageContext.TryGet<ICommonSession>(DefaultChannelParameters.SenderSession, out var sender))
            return message;

        var sessionName = sender.Name;
        message.InsertBeforeMessage(new MarkupNode("SessionNameHeader", new MarkupParameter(sessionName), null));

        return message;
    }
}
