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

    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var sender) && sender is EntityUid)
        {
            var senderEntity = (EntityUid)sender;
            MetaDataComponent? metaData = null;
            string? name;

            if (!_entityManager.MetaQuery.Resolve(senderEntity, ref metaData, false))
            {
                return;
            }

            name = metaData.EntityName;

            var nameEv = new TransformSpeakerNameEvent(senderEntity, name);
            _entityManager.EventBus.RaiseLocalEvent(senderEntity, nameEv);

            message.InsertBeforeMessage(new MarkupNode("EntityNameHeader", new MarkupParameter(nameEv.VoiceName), null));
        }
    }
}
