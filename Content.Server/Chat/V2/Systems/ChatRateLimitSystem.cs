using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Systems;
using Content.Shared.Database;
using Content.Shared.Players;
using Robust.Shared.Configuration;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace Content.Server.Chat.V2.Systems;

public sealed class ChatRateLimitSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private int _periodLength;
    private bool _chatRateLimitAnnounceAdmins;
    private int _chatRateLimitAnnounceAdminDelay;
    private int _chatRateLimitCount;
    private int _maxChatMessageLength;

    public override void Initialize()
    {
        base.Initialize();

        _configuration.OnValueChanged(CCVars.ChatRateLimitPeriod, periodLength => _periodLength = periodLength, true);
        _configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdmins, announce => _chatRateLimitAnnounceAdmins = announce, true);
        _configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdminsDelay, announce => _chatRateLimitAnnounceAdminDelay = announce, true);
        _configuration.OnValueChanged(CCVars.ChatRateLimitCount, limitCount => _chatRateLimitCount = limitCount, true);
        _configuration.OnValueChanged(CCVars.ChatMaxAnnouncementLength, maxLen => _maxChatMessageLength = maxLen, true);

        SubscribeLocalEvent<ChatSentEvent<SendableChatEvent>>(OnValidationEvent);
    }

    private void OnValidationEvent(ChatSentEvent<SendableChatEvent> validationEvent, EntitySessionEventArgs args)
    {
        if (validationEvent.IsCancelled)
            return;

        if (IsRateLimited(GetEntity(validationEvent.Event.Sender), validationEvent.Event.Message.Length, out var reason))
        {
            validationEvent.Cancel(reason);
        }
    }

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
            data.MessageRateOverTime /= 2;
            data.NetMessageLengthOverTime /= 2;

            data.RateLimitAnnouncedToPlayer = false;
        }

        data.MessageRateOverTime += 1;
        data.NetMessageLengthOverTime += messageLen;

        if (data.MessageRateOverTime <= _chatRateLimitCount && data.NetMessageLengthOverTime <= _maxChatMessageLength)
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
