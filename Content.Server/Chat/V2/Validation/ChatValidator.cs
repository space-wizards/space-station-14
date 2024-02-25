using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.V2.Repository;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Radio;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Chat.V2.Validation;

public sealed class ChatValidator : EntitySystem
{
    [Dependency] private readonly ChatRepository _repo = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private int _maxChatMessageLength;
    private int _periodLength;
    private bool _chatRateLimitAnnounceAdmins;
    private int _chatRateLimitAnnounceAdminDelay;
    private int _chatRateLimitCount;

    public override void Initialize()
    {
        base.Initialize();

        _configuration.OnValueChanged(CCVars.ChatRateLimitPeriod, periodLength => _periodLength = periodLength, true);
        _configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdmins, announce => _chatRateLimitAnnounceAdmins = announce, true);
        _configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdminsDelay, announce => _chatRateLimitAnnounceAdminDelay = announce, true);
        _configuration.OnValueChanged(CCVars.ChatRateLimitCount, limitCount => _chatRateLimitCount = limitCount, true);
        _configuration.OnValueChanged(CCVars.ChatMaxAnnouncementLength, maxLen => _maxChatMessageLength = maxLen, true);

        SubscribeNetworkEvent<AttemptDeadChatEvent>(HandleAttemptDeadChatMessage);
        SubscribeNetworkEvent<AttemptEmoteEvent>(HandleAttemptEmoteMessage);
        SubscribeNetworkEvent<AttemptLocalChatEvent>(HandleAttemptLocalChatMessage);
        SubscribeNetworkEvent<AttemptLoocEvent>(HandleAttemptLoocMessage);
        SubscribeNetworkEvent<AttemptHeadsetRadioEvent>(HandleAttemptHeadsetRadioMessage);
        SubscribeNetworkEvent<AttemptInternalRadioEvent>(HandleAttemptInternalRadioMessage);
        SubscribeNetworkEvent<AttemptWhisperEvent>(HandleAttemptWhisperEvent);
    }

    private void HandleAttemptDeadChatMessage(AttemptDeadChatEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Speaker);

        if (!IsMessageValid(ev, args.SenderSession, entityUid, ev.Message, out var reason))
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(ev.Speaker, reason), args.SenderSession);

            return;
        }

        var isAdmin = _admin.IsAdmin(entityUid);

        // Dead Chat doesn't use a sentinel component to control access.
        if (!_admin.IsAdmin(entityUid) || !HasComp<GhostComponent>(entityUid) && !_mobState.IsDead(entityUid))
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(ev.Speaker, Loc.GetString("chat-system-dead-chat-failed")), args.SenderSession);

            return;
        }

        _repo.Add(new DeadChatCreatedEvent(entityUid, ev.Message, isAdmin));
    }

    private void HandleAttemptEmoteMessage(AttemptEmoteEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Emoter);
        if (!IsMessageValid(
                ev,
                args.SenderSession,
                entityUid,
                ev.Message,
                out var reason, "chat-system-emote-failed",
                out EmoteableComponent? comp))
        {
            RaiseNetworkEvent(new EmoteFailedEvent(ev.Emoter, reason), args.SenderSession);

            return;
        }

        _repo.Add(new EmoteCreatedEvent(entityUid, ev.Message, comp.Range));
    }

    private void HandleAttemptLocalChatMessage(AttemptLocalChatEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Speaker);
        if (!IsMessageValid(
                ev,
                args.SenderSession,
                entityUid,
                ev.Message,
                out var reason,
                "chat-system-local-chat-failed",
                out LocalChattableComponent? comp))
        {
            RaiseNetworkEvent(new LocalChatFailedEvent(ev.Speaker, reason), args.SenderSession);

            return;
        }

        _repo.Add(new LocalChatCreatedEvent(entityUid, ev.Message, comp.Range));
    }

    private void HandleAttemptLoocMessage(AttemptLoocEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Speaker);
        if (!IsMessageValid(ev, args.SenderSession, entityUid, ev.Message, out var reason))
        {
            RaiseNetworkEvent(new LoocFailedEvent(ev.Speaker, reason), args.SenderSession);

            return;
        }

        _repo.Add(new LoocCreatedEvent(entityUid, ev.Message));
    }

    private void HandleAttemptHeadsetRadioMessage(AttemptHeadsetRadioEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Speaker);
        if (!IsMessageValid(
                ev,
                args.SenderSession,
                entityUid,
                ev.Message,
                out var reason, "chat-system-radio-failed",
                out HeadsetRadioableComponent? comp))
        {
            RaiseNetworkEvent(new RadioFailedEvent(ev.Speaker, reason), args.SenderSession);

            return;
        }

        HandleRadioMessage(ev.Channel, ev.Speaker, ev.Message, args, comp.Channels, entityUid);
    }

    private void HandleAttemptInternalRadioMessage(AttemptInternalRadioEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Speaker);
        if (!IsMessageValid(
                ev,
                args.SenderSession,
                entityUid,
                ev.Message,
                out var reason,
                "chat-system-radio-failed",
                out InternalRadioComponent? comp))
        {
            RaiseNetworkEvent(new RadioFailedEvent(ev.Speaker, reason), args.SenderSession);

            return;
        }

        HandleRadioMessage(ev.Channel, ev.Speaker, ev.Message, args, comp.SendChannels, entityUid);
    }

    private void HandleRadioMessage(string channel, NetEntity speaker, string message, EntitySessionEventArgs args, HashSet<string> channels, EntityUid entityUid) {
        // The channel might not exist
        if (!_proto.TryIndex(channel, out RadioChannelPrototype? radioChannelProto))
        {
            RaiseNetworkEvent(
                new RadioFailedEvent(speaker,
                    Loc.GetString("chat-system-radio-channel-nonexistent", ("channel", channel))), args.SenderSession);

            return;
        }

        // You might not be able to radio
        if (!channels.Contains(channel))
        {
            RaiseNetworkEvent(
                new RadioFailedEvent(speaker,
                    Loc.GetString("chat-system-radio-channel-failed", ("channel", channel))), args.SenderSession);

            return;
        }

        _repo.Add(new RadioCreatedEvent(entityUid, message, radioChannelProto));
    }

    private void HandleAttemptWhisperEvent(AttemptWhisperEvent ev, EntitySessionEventArgs args)
    {
        var entityUid = GetEntity(ev.Speaker);
        if (!IsMessageValid(
                ev,
                args.SenderSession,
                entityUid,
                ev.Message,
                out var reason,
                "chat-system-whisper-failed",
                out WhisperableComponent? comp))
        {
            RaiseNetworkEvent(new WhisperFailedEvent(ev.Speaker, reason), args.SenderSession);

            return;
        }

        _repo.Add(new WhisperCreatedEvent(entityUid, ev.Message, comp.MinRange, comp.MaxRange));
    }

    private bool IsMessageValid<T>(T ev, ICommonSession player, EntityUid entity, string message, out string reason)
    {
        if (!IsOwnedBy(entity, player))
        {
            reason = Loc.GetString("chat-system-entity-not-owned-by-sender");

            return false;
        }

        if (IsRateLimited(entity, message.Length, out reason))
        {
            return false;
        }

        if (message.Length > _maxChatMessageLength)
        {
            reason = Loc.GetString("chat-system-max-message-length-exceeded-message");

            return false;
        }

        var validate = new ValidationEvent<T>(ev);
        RaiseLocalEvent(entity, validate, true);
        if (validate.IsCancelled)
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(GetNetEntity(entity), validate.Reason), player);

            return false;
        }

        return true;
    }

    private bool IsMessageValid<T1, T2>(
        T1 ev,
        ICommonSession player,
        EntityUid entity,
        string message,
        out string reason,
        string invalidCompReason,
        [NotNullWhen(true)] out T2? comp
    ) where T2 : IComponent
    {
        comp = default;

        if (!IsMessageValid(ev, player, entity, message, out reason))
        {
            return false;
        }

        if (!TryComp(entity, out comp))
        {
            reason = Loc.GetString(invalidCompReason);

            return false;
        }

        return true;
    }

    private bool IsOwnedBy(EntityUid entity, ICommonSession session)
    {
        return session.AttachedEntity != null && session.AttachedEntity == entity;
    }

    // TODO: Expose this if it's useful for other classes.
    private bool IsRateLimited(EntityUid entityUid, int messageLen, out string reason)
    {
        reason = "";

        if (!_playerManager.TryGetSessionByEntity(entityUid, out var session))
            return false;

        var data = session.ContentData();

        if (data == null)
            return false;

        var time = _gameTiming.RealTime;

        if (data.MessageCountExpiresAt < time)
        {
            data.MessageCountExpiresAt = time + TimeSpan.FromSeconds(_periodLength);

            // Backoff from spamming slowly
            data.MessageCount /= 2;
            data.TotalMessageLength /= 2;

            data.RateLimitAnnouncedToPlayer = false;
        }

        data.MessageCount += 1;
        data.TotalMessageLength += messageLen;

        if (data.MessageCount <= _chatRateLimitCount && data.TotalMessageLength <= _maxChatMessageLength)
            return false;

        // Breached rate limits, inform admins if configured.
        if (_chatRateLimitAnnounceAdmins)
        {
            if (data.CanAnnounceToAdminsNextAt < time)
            {
                _chatManager.SendAdminAlert(Loc.GetString("chat-manager-rate-limit-admin-announcement", ("player", session.Name)));

                data.CanAnnounceToAdminsNextAt = time + TimeSpan.FromSeconds(_chatRateLimitAnnounceAdminDelay);
            }
        }

        if (data.RateLimitAnnouncedToPlayer)
            return true;

        reason = Loc.GetString(Loc.GetString("chat-manager-rate-limited"));

        _adminLogger.Add(LogType.ChatRateLimited, LogImpact.Medium, $"Player {session} breached chat rate limits");

        data.RateLimitAnnouncedToPlayer = true;

        return true;
    }
}

public sealed class ValidationEvent<T>
{
    public readonly T Event;
    public string Reason = "";

    public ValidationEvent(T attemptEvent)
    {
        Event = attemptEvent;
    }
    public bool IsCancelled { get; private set; }

    public void Cancel(string reason)
    {
        if (IsCancelled)
        {
            return;
        }

        IsCancelled = true;
        Reason = reason;
    }
}
