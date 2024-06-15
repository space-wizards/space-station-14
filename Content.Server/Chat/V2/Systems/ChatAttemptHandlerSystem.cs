using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Systems;

namespace Content.Server.Chat.V2.Systems;

/// <summary>
/// ChatAttemptHandlerSystem defines a set of handlers and processes for managing chat attempts sent by clients.
/// </summary>
public sealed class ChatAttemptHandlerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AttemptVerbalChatEvent>(OnAttemptChat<AttemptVerbalChatEvent, VerbalChatCreatedEvent>);
        SubscribeNetworkEvent<AttemptVisualChatEvent>(OnAttemptChat<AttemptVisualChatEvent, VisualChatCreatedEvent>);
        SubscribeNetworkEvent<AttemptAnnouncementEvent>(OnAttemptChat<AttemptAnnouncementEvent, AnnouncementCreatedEvent>);
        SubscribeNetworkEvent<AttemptOutOfCharacterChatEvent>(OnAttemptChat<AttemptOutOfCharacterChatEvent, OutOfCharacterChatCreatedEvent>);
    }

    private void OnAttemptChat<T1, T2>(T1 ev, EntitySessionEventArgs args) where T1 : ChatAttemptEvent where T2 : ChatEvent
    {
        if (!ValidateMessage(ev, out var reason))
        {
            RaiseNetworkEvent(ev.ToFailMessage(reason), args.SenderSession);

            return;
        }

        var success = ev.ToCreatedEvent(Name(GetEntity(ev.Sender)));
        if (success is not T2 typedSuccess)
            return;

        SanitizeMessage(success);
        RaiseLocalEvent(new ChatAttemptValidatedEvent<T2>(typedSuccess));
    }

    /// <summary>
    /// Validates messages. Return true means the message is valid.
    /// </summary>
    private bool ValidateMessage<T>(T ev, out string reason) where T : ChatAttemptEvent
    {
        var entityUid = GetEntity(ev.Sender);

        // Raise both the general and specific ChatValidationEvents. This allows for general
        var validate = new ChatValidationEvent<ChatAttemptEvent>(ev);
        RaiseLocalEvent(entityUid, ref validate);

        if (validate.IsCancelled)
        {
            reason = validate.Reason;

            return false;
        }

        var validateT = new ChatValidationEvent<T>(ev);
        RaiseLocalEvent(entityUid, ref validateT);

        if (validate.IsCancelled)
        {
            reason = validate.Reason;

            return false;
        }

        reason = "";

        return true;
    }

    /// <summary>
    /// Sanitizes messages. The return string is sanitized.
    /// </summary>
    private void SanitizeMessage<T>(T evt) where T : IChatEvent
    {
        var sanitize = new ChatSanitizationEvent<T>(evt);
        RaiseLocalEvent(sanitize);

        evt.Message = sanitize.ChatMessageSanitized ?? sanitize.ChatMessageRaw;
    }
}
