using System.Globalization;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Chat.V2.Censorship;
using Content.Server.Speech.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _logger = default!;
    [Dependency] private readonly IEmoteConfigManager _emoteConfig = default!;
    [Dependency] private readonly IChatUtilities _chatUtilities = default!;
    [Dependency] private readonly ReplacementAccentSystem _repAccent = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private bool _shouldCapitalizeTheWordI;
    private bool _shouldPunctuate;
    private int _maxChatMessageLength;
    private int _chatRateLimitCount;
    private int _periodLength;
    private bool _chatRateLimitAnnounceAdmins;
    private int _chatRateLimitAnnounceAdminDelay;

    public override void Initialize()
    {
        base.Initialize();

        _shouldCapitalizeTheWordI = (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
                                   || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en");

        _shouldPunctuate = _configuration.GetCVar(CCVars.ChatPunctuation);
        _maxChatMessageLength = _configuration.GetCVar(CCVars.ChatMaxMessageLength);
        _chatRateLimitCount = _configuration.GetCVar(CCVars.ChatRateLimitCount);
        _periodLength = _configuration.GetCVar(CCVars.ChatRateLimitPeriod);
        _chatRateLimitAnnounceAdmins = _configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdmins);
        _chatRateLimitAnnounceAdminDelay = _configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdminsDelay);

        _configuration.OnValueChanged(CCVars.ChatPunctuation, shouldPunctuate => _shouldPunctuate = shouldPunctuate);
        _configuration.OnValueChanged(CCVars.ChatMaxMessageLength, maxLen => _maxChatMessageLength = maxLen);
        _configuration.OnValueChanged(CCVars.ChatRateLimitCount, limitCount => _chatRateLimitCount = limitCount);
        _configuration.OnValueChanged(CCVars.ChatRateLimitPeriod, periodLength => _periodLength = periodLength);
        _configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdmins, announce => _chatRateLimitAnnounceAdmins = announce);

        InitializeDeadChat();
        InitializeEmoting();
        InitializeLocalChat();
        InitializeLoocChat();
        InitializeRadio();
        InitializeWhisper();
    }

    private string SanitizeInCharacterMessage(EntityUid source, string message, out string? emoteStr)
    {
        message = message.Trim();
        ChatCensor.Censor(message, out message);
        message = _repAccent.ApplyReplacements(message, "chatsanitize");
        message = _chatUtilities.CapitalizeFirstLetter(message);

        if (_shouldCapitalizeTheWordI)
            message = _chatUtilities.CapitalizeIPronoun(message);

        if (_shouldPunctuate)
            message = _chatUtilities.AddAPeriod(message);

        _sanitizer.TrySanitizeOutSmilies(message, source, out message, out emoteStr);

        return message;
    }

    private string SanitizeMessage(string message, bool punctuate = false)
    {
        message = message.Trim();
        ChatCensor.Censor(message, out message);
        message = _chatUtilities.CapitalizeFirstLetter(message);

        if (_shouldCapitalizeTheWordI)
            message = _chatUtilities.CapitalizeIPronoun(message);

        if (punctuate)
            message = _chatUtilities.AddAPeriod(message);

        return message;
    }

    private string TransformSpeech(EntityUid sender, string message)
    {
        var ev = new TransformSpeechEvent(sender, message);
        RaiseLocalEvent(ev);

        return ev.Message;
    }

    private string GetSpeakerName(EntityUid entityToBeNamed)
    {
        var nameEv = new TransformSpeakerNameEvent(entityToBeNamed, Name(entityToBeNamed));
        RaiseLocalEvent(entityToBeNamed, nameEv);

        return nameEv.Name;
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

                var delay = _configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdminsDelay);
                data.CanAnnounceToAdminsNextAt = time + TimeSpan.FromSeconds(delay);
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
