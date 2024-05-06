using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Managers;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
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
    private const string EmoteFailed = "chat-system-emote-failed";
    private const string LocalChatFailed = "chat-system-local-chat-failed";
    private const string RadioFailed = "chat-system-radio-failed";
    private const string WhisperFailed = "chat-system-whisper-failed";
    private const string EntityNotOwnedBySender = "chat-system-entity-not-owned-by-sender";

    [Dependency] private readonly ChatRepositorySystem _repo = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private string _deadChatFailed = "";
    private string _emoteFailed = "";
    private string _localChatFailed = "";
    private string _radioFailed = "";
    private string _whisperFailed = "";
    private string _entityNotOwnedBySender = "";

    public override void Initialize()
    {
        base.Initialize();

        _deadChatFailed = Loc.GetString(DeadChatFailed);
        _emoteFailed = Loc.GetString(EmoteFailed);
        _localChatFailed = Loc.GetString(LocalChatFailed);
        _radioFailed = Loc.GetString(RadioFailed);
        _whisperFailed = Loc.GetString(WhisperFailed);
        _entityNotOwnedBySender = Loc.GetString(EntityNotOwnedBySender);

        SubscribeNetworkEvent<AttemptDeadChatEvent>(HandleAttemptDeadChat);
        SubscribeNetworkEvent<AttemptEmoteEvent>(HandleAttemptEmote);
        SubscribeNetworkEvent<AttemptLocalChatEvent>(HandleAttemptLocalChat);
        SubscribeNetworkEvent<AttemptLoocEvent>(HandleAttemptLooc);
        SubscribeNetworkEvent<AttemptEquipmentRadioEvent>(HandleAttemptEquipmentRadio);
        SubscribeNetworkEvent<AttemptInternalRadioEvent>(HandleAttemptInternalRadio);
        SubscribeNetworkEvent<AttemptWhisperEvent>(HandleAttemptWhisper);
    }

    private void HandleAttemptDeadChat(AttemptDeadChatEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Sender);

        if (!ValidateMessage(ev, args.SenderSession, out var reason))
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(ev.Sender, reason), args.SenderSession);

            return;
        }

        var isAdmin = _admin.IsAdmin(entityUid);

        // Dead Chat doesn't use a sentinel component to control access. Admins can always talk on Dead Chat.
        if (!isAdmin)
        {
            // Non-admins can only talk on dead chat if they're a ghost or currently dead.
            if (!HasComp<GhostComponent>(entityUid) && !_mobState.IsDead(entityUid))
            {
                RaiseNetworkEvent(new DeadChatFailedEvent(ev.Sender, _deadChatFailed),
                    args.SenderSession);
            }

            return;
        }

        _repo.Add(new DeadChatCreatedEvent(entityUid, SanitizeMessage(ev), isAdmin));
    }

    private void HandleAttemptEmote(AttemptEmoteEvent ev, EntitySessionEventArgs args)
    {
        if (!ValidateMessage(ev, args.SenderSession, out var reason, _emoteFailed, out CanEmoteComponent? comp))
        {
            RaiseNetworkEvent(new EmoteFailedEvent(ev.Sender, reason), args.SenderSession);

            return;
        }

        _repo.Add(new EmoteCreatedEvent(GetEntity(ev.Sender), SanitizeMessage(ev), comp.Range));
    }

    private void HandleAttemptLocalChat(AttemptLocalChatEvent ev, EntitySessionEventArgs args)
    {
        if (!ValidateMessage(ev, args.SenderSession, out var reason, _localChatFailed, out CanLocalChatComponent? comp))
        {
            RaiseNetworkEvent(new LocalChatFailedEvent(ev.Sender, reason), args.SenderSession);

            return;
        }

        _repo.Add(new LocalChatCreatedEvent(GetEntity(ev.Sender), SanitizeMessage(ev), comp.Range));
    }

    private void HandleAttemptLooc(AttemptLoocEvent ev, EntitySessionEventArgs args)
    {
        if (!ValidateMessage(ev, args.SenderSession, out var reason))
        {
            RaiseNetworkEvent(new LoocFailedEvent(ev.Sender, reason), args.SenderSession);

            return;
        }

        _repo.Add(new LoocCreatedEvent(GetEntity(ev.Sender), SanitizeMessage(ev)));
    }

    private void HandleAttemptEquipmentRadio(AttemptEquipmentRadioEvent ev, EntitySessionEventArgs args)
    {
        if (!ValidateMessage(ev, args.SenderSession, out var reason, _radioFailed,
                out CanRadioUsingEquipmentComponent? comp))
        {
            RaiseNetworkEvent(new RadioFailedEvent(ev.Sender, reason), args.SenderSession);

            return;
        }

        _repo.Add(new RadioCreatedEvent(GetEntity(ev.Sender), SanitizeMessage(ev), ev.Channel));
    }

    private void HandleAttemptInternalRadio(AttemptInternalRadioEvent ev, EntitySessionEventArgs args)
    {
        if (!ValidateMessage(ev, args.SenderSession, out var reason, _radioFailed, out CanRadioComponent? comp))
        {
            RaiseNetworkEvent(new RadioFailedEvent(ev.Sender, reason), args.SenderSession);

            return;
        }

        _repo.Add(new RadioCreatedEvent(GetEntity(ev.Sender), SanitizeMessage(ev), ev.Channel));
    }

    private void HandleAttemptWhisper(AttemptWhisperEvent ev, EntitySessionEventArgs args)
    {
        if (!ValidateMessage(ev, args.SenderSession, out var reason, _whisperFailed, out CanWhisperComponent? comp))
        {
            RaiseNetworkEvent(new WhisperFailedEvent(ev.Sender, reason), args.SenderSession);

            return;
        }

        _repo.Add(new WhisperCreatedEvent(GetEntity(ev.Sender), SanitizeMessage(ev), comp.MinRange, comp.MaxRange));
    }

    /// <summary>
    /// Validates messages. Return true means the message is valid.
    /// </summary>
    private bool ValidateMessage<T>(T ev, ICommonSession player, out string reason) where T : ChatAttemptEvent
    {
        var entityUid = GetEntity(ev.Sender);

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
        string invalidCompReason,
        [NotNullWhen(true)] out T2? comp
    ) where T1 : ChatAttemptEvent where T2 : IComponent
    {
        comp = default;

        var entityUid = GetEntity(ev.Sender);

        if (!ValidateMessage(ev, player, out reason))
        {
            return false;
        }

        if (TryComp(entityUid, out comp))
            return true;

        reason = Loc.GetString(invalidCompReason);

        return false;
    }

    /// <summary>
    /// Sanitizes messages. The return string is sanitized.
    /// </summary>
    private string SanitizeMessage<T>(T evt) where T : ChatAttemptEvent
    {
        var sanitize = new ChatSanitizationEvent<T>(evt);
        RaiseLocalEvent(ref sanitize);

        return sanitize.ChatMessageSanitized ?? sanitize.ChatMessageRaw;
    }
}
