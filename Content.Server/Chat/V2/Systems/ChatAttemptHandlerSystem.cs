using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Chat.V2.Systems;
using Robust.Shared.Player;

namespace Content.Server.Chat.V2.Systems;

/// <summary>
/// ChatAttemptHandlerSystem defines a set of handlers and processes for managing chat attempts sent by clients.
/// </summary>
public sealed class ChatAttemptHandlerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AttemptVerbalChatEvent>(OnAttemptChat);
        SubscribeNetworkEvent<AttemptVisualChatEvent>(OnAttemptChat);
        SubscribeNetworkEvent<AttemptAnnouncementEvent>(OnAttemptChat);
        SubscribeNetworkEvent<AttemptOutOfCharacterChatEvent>(OnAttemptChat);
    }

    private void OnAttemptChat(ChatAttemptEvent ev, EntitySessionEventArgs args)
    {
        if (!ValidateMessage(ev, out var reason))
        {
            RaiseNetworkEvent(ev.ToFailMessage(reason), args.SenderSession);

            return;
        }

        var success = ev.ToSuccessMessage();

        SanitizeMessage(success);
        RaiseLocalEvent(new ChatAttemptValidatedEvent(success));
    }

    /// <summary>
    /// Validates messages. Return true means the message is valid.
    /// </summary>
    private bool ValidateMessage<T>(T ev, out string reason) where T : ChatAttemptEvent
    {
        var entityUid = GetEntity(ev.Sender);

        // Raise both the general and specific ChatValidationEvents. This allows for general
        var validate = new ChatValidationEvent<ChatAttemptEvent>(ev);
        RaiseLocalEvent(entityUid, validate);

        if (validate.IsCancelled)
        {
            reason = validate.Reason;

            return false;
        }

        var validateT = new ChatValidationEvent<T>(ev);
        RaiseLocalEvent(entityUid, validateT);

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
