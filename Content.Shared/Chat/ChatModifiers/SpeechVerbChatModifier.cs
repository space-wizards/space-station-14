using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds the appropriate speech verb after the speaker's name, e.g.
/// "x says, "
/// "y exclaims, "
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpeechVerbChatModifier : ChatModifier
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public ProtoId<SpeechVerbPrototype> DefaultSpeechVerb = "Default";

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        if (!chatMessageContext.TryGet<EntityUid>(DefaultChannelParameters.SenderEntity, out var sender) ||
            !chatMessageContext.TryGet<int>(DefaultChannelParameters.RandomSeed, out var seed)
        )
            return message;

        IoCManager.InjectDependencies(this);

        //Name property doesn't matter, we're only interested in the speech
        var transformEv = new TransformSpeakerNameEvent(sender, String.Empty);
        _entityManager.EventBus.RaiseLocalEvent(sender, transformEv);

        var current = GetSpeechVerbProto(message, transformEv.SpeechVerb, sender);

        var count = current.SpeechVerbStrings.Count;

        _random.SetSeed(seed);
        var verbId = _random.Next(count);

        // if no applicable suffix verb return the normal one used by the entity
        var node = new MarkupNode("SpeechVerb",
            new MarkupParameter(current.ID),
            new Dictionary<string, MarkupParameter> { { "id", new MarkupParameter(verbId) } }
        );

        message.InsertAfterTag(node, "EntityNameHeader");
        return message;
    }

    private SpeechVerbPrototype GetSpeechVerbProto(FormattedMessage message, ProtoId<SpeechVerbPrototype>? speechVerb, EntityUid sender)
    {
        // This if/else tree can probably be cleaned up at some point
        if (speechVerb != null && _prototypeManager.TryIndex(speechVerb, out var eventProto))
        {
            return eventProto;
        }

        if (!_entityManager.TryGetComponent<SpeechComponent>(sender, out var speech))
        {
            return _prototypeManager.Index<SpeechVerbPrototype>(DefaultSpeechVerb);
        }

        SpeechVerbPrototype? current = null;
        // check for a suffix-applicable speech verb
        foreach (var (str, id) in speech.SuffixSpeechVerbs)
        {
            var proto = _prototypeManager.Index<SpeechVerbPrototype>(id);
            if (message.ToString().EndsWith(Loc.GetString(str)) &&
                proto.Priority >= (current?.Priority ?? 0))
            {
                current = proto;
            }
        }

        // if no applicable suffix verb return the normal one used by the entity
        current ??= _prototypeManager.Index<SpeechVerbPrototype>(speech.SpeechVerb);

        return current;
    }
}
