using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Managers;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Chat.V2.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;

namespace Content.Server.Chat.V2.Systems;

/// <summary>
/// ChatAttemptHandlerSystem defines a set of handlers and processes for managing chat attempts sent by clients.
/// </summary>
public sealed class ChatAttemptHandlerSystem : EntitySystem
{
    private const string DeadChatFailed = "chat-system-dead-chat-failed";
    private const string EntityNotOwnedBySender = "chat-system-entity-not-owned-by-sender";
    private const string UnsupportedMessageType = "chat-system-unsupported-message-type";
    private const string MissingRequiredComponent = "chat-system-missing-required-component";

    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private string _deadChatFailed = "";
    private string _entityNotOwnedBySender = "";
    private string _unsupportedMessageType = "";
    private string _missingRequiredComponent = "";

    public override void Initialize()
    {
        base.Initialize();

        _deadChatFailed = Loc.GetString(DeadChatFailed);
        _entityNotOwnedBySender = Loc.GetString(EntityNotOwnedBySender);
        _unsupportedMessageType = Loc.GetString(UnsupportedMessageType);
        _missingRequiredComponent = Loc.GetString(MissingRequiredComponent);

        SubscribeNetworkEvent<AttemptVerbalChatEvent>((ev, args) => OnAttemptChat<CanLocalChatComponent>(ev, args, new VerbalChatFailedEvent()));
        SubscribeNetworkEvent<AttemptEmoteEvent>((ev, args) => OnAttemptChat<CanEmoteComponent>(ev, args, new EmoteFailedEvent()));
        SubscribeNetworkEvent<AttemptAnnouncementEvent>(OnAttemptChat);
        SubscribeNetworkEvent<AttemptOutOfCharacterChatEvent>(OnAttemptChat);
    }

    private void OnAttemptChat<T>(ChatAttemptEvent ev, EntitySessionEventArgs args, ChatFailedEvent failEv) where T : Component
    {
        if (!ValidateMessage(ev, args.SenderSession, out var reason, out T? _))
        {
            OnFailedMessage(failEv, ev.Sender, reason, args);

            return;
        }

        OnValidatedMessage(ev);
    }

    private void OnAttemptChat(AttemptAnnouncementEvent ev, EntitySessionEventArgs args)
    {
        if (!ValidateMessage(GetEntity(ev.Sender), ev, args.SenderSession, out var reason))
        {
            OnFailedMessage(new AnnouncementFailedEvent(), ev.Sender, reason, args);

            return;
        }

        OnValidatedMessage(ev);
    }

    private void OnAttemptChat(AttemptOutOfCharacterChatEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Sender);

        switch (ev.Channel)
        {
            case OutOfCharacterChatChannel.Dead:
                if (!IsDeadChatValid(ev, args, entityUid))
                    return;

                break;
            case OutOfCharacterChatChannel.OutOfCharacter:
            case OutOfCharacterChatChannel.System:
            case OutOfCharacterChatChannel.Admin:
            default:
                OnFailedMessage(new OutOfCharacterChatFailed(), ev.Sender, _unsupportedMessageType, args);

                return;
        }

        OnValidatedMessage(ev);
    }

    private void OnValidatedMessage(ChatAttemptEvent ev)
    {
        SanitizeMessage(ev);
        RaiseLocalEvent(new ChatAttemptValidatedEvent(ev));
    }

    private bool IsDeadChatValid(AttemptOutOfCharacterChatEvent ev, EntitySessionEventArgs args, EntityUid entityUid)
    {
        var isAdmin = _admin.IsAdmin(entityUid);

        // Non-admins can only talk on dead chat if they're a ghost or currently dead.
        if (!isAdmin && !HasComp<GhostComponent>(entityUid) && !_mobState.IsDead(entityUid))
        {
            OnFailedMessage(new OutOfCharacterChatFailed(), ev.Sender, _deadChatFailed, args);

            return false;
        }
        // ReSharper disable once InvertIf
        if (!ValidateMessage(entityUid, ev, args.SenderSession, out var reason))
        {
            OnFailedMessage(new OutOfCharacterChatFailed(), ev.Sender, reason, args);

            return false;
        }

        return true;
    }

    public void OnFailedMessage(ChatFailedEvent ev, NetEntity entity, string reason, EntitySessionEventArgs args)
    {
        ev.Sender = entity;
        ev.Reason = reason;

        RaiseNetworkEvent(ev, args.SenderSession);
    }

    /// <summary>
    /// Validates messages. Return true means the message is valid.
    /// </summary>
    private bool ValidateMessage<T>(EntityUid entityUid, T ev, ICommonSession player, out string reason) where T : ChatAttemptEvent
    {
        // This check is simple, so we don't need to raise an event for it.
        if (player.AttachedEntity != null || player.AttachedEntity != entityUid)
        {
            reason = _entityNotOwnedBySender;

            return false;
        }

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
    /// Validates messages which depend on a sentinel component to be legal.
    /// </summary>
    private bool ValidateMessage<T1, T2>(
        T1 ev,
        ICommonSession player,
        out string reason,
        [NotNullWhen(true)] out T2? comp
    ) where T1 : ChatAttemptEvent where T2 : IComponent
    {
        comp = default;

        var entityUid = GetEntity(ev.Sender);

        if (!ValidateMessage(entityUid, ev, player, out reason))
        {
            return false;
        }

        if (TryComp(entityUid, out comp))
            return true;

        reason = _missingRequiredComponent;

        return false;
    }

    /// <summary>
    /// Sanitizes messages. The return string is sanitized.
    /// </summary>
    private void SanitizeMessage<T>(T evt) where T : ChatAttemptEvent
    {
        var sanitize = new ChatSanitizationEvent<T>(evt);
        RaiseLocalEvent(sanitize);

        evt.Message = sanitize.ChatMessageSanitized ?? sanitize.ChatMessageRaw;
    }
}
