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

        _maxChatMessageLength = _configuration.GetCVar(CCVars.ChatMaxMessageLength);
        _periodLength = _configuration.GetCVar(CCVars.ChatRateLimitPeriod);
        _chatRateLimitAnnounceAdmins = _configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdmins);
        _chatRateLimitAnnounceAdminDelay = _configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdminsDelay);
        _chatRateLimitCount = _configuration.GetCVar(CCVars.ChatRateLimitCount);

        _configuration.OnValueChanged(CCVars.ChatRateLimitPeriod, periodLength => _periodLength = periodLength);
        _configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdmins, announce => _chatRateLimitAnnounceAdmins = announce);
        _configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdminsDelay, announce => _chatRateLimitAnnounceAdminDelay = announce);
        _configuration.OnValueChanged(CCVars.ChatRateLimitCount, limitCount => _chatRateLimitCount = limitCount);
        _configuration.OnValueChanged(CCVars.ChatMaxAnnouncementLength, maxLen => _maxChatMessageLength = maxLen);

        SubscribeNetworkEvent<AttemptDeadChatEvent>((msg, args) => { HandleAttemptDeadChatMessage(args.SenderSession, msg.Speaker, msg.Message); });
        SubscribeNetworkEvent<AttemptEmoteEvent>((msg, args) => { HandleAttemptEmoteMessage(args.SenderSession, msg.Emoter, msg.Message); });
        SubscribeNetworkEvent<AttemptLocalChatEvent>((msg, args) => { HandleAttemptLocalChatMessage(args.SenderSession, msg.Speaker, msg.Message); });
        SubscribeNetworkEvent<AttemptLoocEvent>((msg, args) => { HandleAttemptLoocMessage(args.SenderSession, msg.Speaker, msg.Message); });
        SubscribeNetworkEvent<AttemptHeadsetRadioEvent>((msg, args) => { HandleAttemptHeadsetRadioMessage(args.SenderSession, msg.Speaker, msg.Message, msg.Channel); });
        SubscribeNetworkEvent<AttemptInternalRadioEvent>((msg, args) => { HandleAttemptInnateRadioMessage(args.SenderSession, msg.Speaker, msg.Message, msg.Channel); });
        SubscribeNetworkEvent<AttemptWhisperEvent>((msg, args) => { HandleAttemptWhisperEvent(args.SenderSession, msg.Speaker, msg.Message); });
    }

    public bool IsRateLimited(EntityUid entityUid, out string reason)
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
            data.MessageCount /= 2; // Backoff from spamming slowly
            data.RateLimitAnnouncedToPlayer = false;
        }

        data.MessageCount += 1;

        if (data.MessageCount <= _chatRateLimitCount)
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

    private void HandleAttemptDeadChatMessage(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);
        if (!IsMessageValid(player, entityUid, message, out var reason))
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(entity, reason), player);

            return;
        }

        var isAdmin = _admin.IsAdmin(entityUid);

        if (!_admin.IsAdmin(entityUid) || !HasComp<GhostComponent>(entityUid) && !_mobState.IsDead(entityUid))
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(entity, Loc.GetString("chat-system-dead-chat-failed")), player);

            return;
        }

        _repo.Add(new DeadChatCreatedEvent(entityUid, message, isAdmin));
    }

    private void HandleAttemptEmoteMessage(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);
        if (!IsMessageValid(player, entityUid, message, out var reason))
        {
            RaiseNetworkEvent(new EmoteFailedEvent(entity, reason), player);

            return;
        }

        if (!TryComp<EmoteableComponent>(entityUid, out var emoteable))
        {
            RaiseNetworkEvent(new EmoteFailedEvent(entity, Loc.GetString("chat-system-emote-failed")), player);

            return;
        }

        _repo.Add(new EmoteCreatedEvent(entityUid, message, emoteable.Range));
    }

    private void HandleAttemptLocalChatMessage(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);
        if (!IsMessageValid(player, entityUid, message, out var reason))
        {
            RaiseNetworkEvent(new LocalChatFailedEvent(entity, reason), player);

            return;
        }

        if (!TryComp<LocalChattableComponent>(entityUid, out var comp))
        {
            RaiseNetworkEvent(new LocalChatFailedEvent(entity, Loc.GetString("chat-system-local-chat-failed")), player);

            return;
        }

        _repo.Add(new LocalChatCreatedEvent(entityUid, message, comp.Range));
    }

    private void HandleAttemptLoocMessage(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);
        if (!IsMessageValid(player, entityUid, message, out var reason))
        {
            RaiseNetworkEvent(new EmoteFailedEvent(entity, reason), player);

            return;
        }

        _repo.Add(new LoocCreatedEvent(entityUid, message));
    }

    private void HandleAttemptHeadsetRadioMessage(ICommonSession player, NetEntity entity, string message, string channel)
    {
        var entityUid = GetEntity(entity);
        if (!IsMessageValid(player, entityUid, message, out var reason))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, reason), player);

            return;
        }

        if (!TryComp<HeadsetRadioableComponent>(entityUid, out var comp))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, "You can't talk on any radio channel."), player);

            return;
        }

        if (!comp.Channels.Contains(channel))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, Loc.GetString("chat-system-radio-channel-failed", ("channel", channel))), player);

            return;
        }

        if (!_proto.TryIndex(channel, out RadioChannelPrototype? radioChannelProto))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, Loc.GetString("chat-system-radio-channel-nonexistent", ("channel", channel))), player);

            return;
        }

        _repo.Add(new RadioCreatedEvent(entityUid, message, radioChannelProto));
    }

    private void HandleAttemptInnateRadioMessage(ICommonSession player, NetEntity entity, string message, string channel)
    {
        var entityUid = GetEntity(entity);
        if (!IsMessageValid(player, entityUid, message, out var reason))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, reason), player);

            return;
        }

        if (!TryComp<InternalRadioComponent>(entityUid, out var comp))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, "You can't talk on any radio channel."), player);

            return;
        }

        if (!comp.SendChannels.Contains(channel))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, Loc.GetString("chat-system-radio-channel-failed", ("channel", channel))), player);

            return;
        }

        if (!_proto.TryIndex(channel, out RadioChannelPrototype? radioChannelProto))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, Loc.GetString("chat-system-radio-channel-nonexistent", ("channel", channel))), player);

            return;
        }

        _repo.Add(new RadioCreatedEvent(entityUid, message, radioChannelProto));
    }

    private void HandleAttemptWhisperEvent(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);
        if (!IsMessageValid(player, entityUid, message, out var reason))
        {
            RaiseNetworkEvent(new WhisperFailedEvent(entity, reason), player);

            return;
        }

        if (!TryComp<WhisperableComponent>(entityUid, out var comp))
        {
            RaiseNetworkEvent(new WhisperFailedEvent(entity, Loc.GetString("chat-system-whisper-failed")), player);

            return;
        }

        _repo.Add(new WhisperCreatedEvent(entityUid, message, comp.MinRange, comp.MaxRange));
    }

    private bool IsMessageValid(ICommonSession player, EntityUid entity, string message, out string reason)
    {
        if (!IsOwnedBy(entity, player))
        {
            reason = Loc.GetString("chat-system-entity-not-owned-by-sender");

            return false;
        }

        if (IsRateLimited(entity, out reason))
        {
            return false;
        }

        if (message.Length > _maxChatMessageLength)
        {
            reason = Loc.GetString("chat-system-max-message-length-exceeded-message");

            return false;
        }

        return true;
    }

    private bool IsOwnedBy(EntityUid entity, ICommonSession session)
    {
        return session.AttachedEntity != null && session.AttachedEntity == entity;
    }
}
