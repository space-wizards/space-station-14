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
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (channelParameters.TryGetValue(DefaultChannelParameters.SenderSession, out var sender) && sender is ICommonSession)
        {
            var sessionName = ((ICommonSession)sender).Name;

            message.InsertBeforeMessage(new MarkupNode("SessionNameHeader", new MarkupParameter(sessionName), null));
        }
    }
}
