using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Systems;

namespace Content.Server.Chat.V2.Systems;

/// <summary>
/// This handles created messages and handles the lifecycle of a created message from creation to transmission to clients.
/// </summary>
public sealed class CreatedMessageSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MessageCreatedEvent<VerbalChatCreatedEvent>>(OnMessageCreatedEvent);
        SubscribeLocalEvent<MessageCreatedEvent<VisualChatCreatedEvent>>(OnMessageCreatedEvent);
        SubscribeLocalEvent<MessageCreatedEvent<AnnouncementCreatedEvent>>(OnMessageCreatedEvent);

        SubscribeLocalEvent<MessageCreatedEvent<OutOfCharacterChatCreatedEvent>>(OnOocMessageCreatedEvent);
    }

    private void OnMessageCreatedEvent<T>(MessageCreatedEvent<T> ev, EntitySessionEventArgs args) where T : CreatedChatEvent
    {
        var entity = GetEntity(ev.Event.Sender);

        // Mutate the message: this is to support features like accents...
        var mutationEv = new GeneralChatMutationEvent<T>(ev.Event);
        RaiseLocalEvent(entity, ref mutationEv);

        // Figure out who should receive this event...
        var targetCalculationEv = new ChatTargetCalculationEvent<T>(ev.Event);
        RaiseLocalEvent(entity, ref targetCalculationEv);

        foreach (var target in targetCalculationEv.Targets)
        {
            var outEv = ev.Event.Clone();
            if (outEv is not T chatEvent)
                continue;

            // Mutate the message further for that specific person; this supports features like languages or whisper obfuscation.
            var specificMutationEv = new ChatSpecificMutationEvent<T>(chatEvent, target);
            RaiseLocalEvent(ref specificMutationEv);

            RaiseNetworkEvent(chatEvent.ToReceivedEvent(), target);
        }
    }

    private void OnOocMessageCreatedEvent(MessageCreatedEvent<OutOfCharacterChatCreatedEvent> ev, EntitySessionEventArgs args)
    {
        var targetCalculationEv = new ChatTargetCalculationEvent<OutOfCharacterChatCreatedEvent>(ev.Event);
        RaiseLocalEvent(ref targetCalculationEv);

        foreach (var target in targetCalculationEv.Targets)
        {
            RaiseNetworkEvent(ev.Event.ToReceivedEvent(), target);
        }
    }
}
