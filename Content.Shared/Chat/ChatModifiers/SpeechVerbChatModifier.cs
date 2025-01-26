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

    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var sender) &&
            channelParameters.TryGetValue(DefaultChannelParameters.RandomSeed, out var seed))
        {

            //Name property doesn't matter, we're only interested in the speech
            var transformEv = new TransformSpeakerNameEvent((EntityUid)sender, "");
            _entityManager.EventBus.RaiseLocalEvent((EntityUid)sender, transformEv);

            SpeechVerbPrototype? current = null;

            // This if/else tree can probably be cleaned up at some point
            if (transformEv.SpeechVerb != null && _prototypeManager.TryIndex(transformEv.SpeechVerb, out var evntProto))
            {
                current = evntProto;
            }
            else
            {
                if (!_entityManager.TryGetComponent<SpeechComponent>((EntityUid)sender, out var speech))
                {
                    current = _prototypeManager.Index<SpeechVerbPrototype>(DefaultSpeechVerb);
                }
                else
                {
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
                    current = current ?? _prototypeManager.Index<SpeechVerbPrototype>(speech.SpeechVerb);
                }
            }

            var count = current.SpeechVerbStrings.Count;

            _random.SetSeed((int)seed);
            var verbId = _random.Next(count);

            // if no applicable suffix verb return the normal one used by the entity
            var node = new MarkupNode("SpeechVerb",
                new MarkupParameter(current.ID),
                new Dictionary<string, MarkupParameter>() { { "id", new MarkupParameter(verbId) } }
            );

            message.InsertAfterTag(node, "EntityNameHeader");
        }
    }
}
