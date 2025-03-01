using Content.Shared.Chat.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds a [EntityNameHeader="name"] tag in front of the message.
/// The string inside the tag is based upon the sending entity, and may be changed via certain transformation events (e.g. voicemasks).
/// Once this tag is processed, it gets replaced with the string inside the tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class EntityNameHeaderChatModifier : ChatModifier
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        if (!chatMessageContext.TryGet<EntityUid>(DefaultChannelParameters.SenderEntity, out var sender))
            return message;

        IoCManager.InjectDependencies(this);
        
        MetaDataComponent? metaData = null;
        if (!_entityManager.MetaQuery.Resolve(sender, ref metaData, false))
        {
            return message;
        }

        var nameEv = new TransformSpeakerNameEvent(sender, metaData.EntityName);
        _entityManager.EventBus.RaiseLocalEvent(sender, nameEv);

        var entityNameNode = new MarkupNode("EntityNameHeader", new MarkupParameter(nameEv.VoiceName), null);
        message.InsertBeforeMessage(entityNameNode);

        return message;
    }
}
