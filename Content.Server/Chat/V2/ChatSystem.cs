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
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem : SharedChatSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _logger = default!;
    [Dependency] private readonly ReplacementAccentSystem _repAccent = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private int _periodLength;
    private bool _chatRateLimitAnnounceAdmins;
    private int _chatRateLimitAnnounceAdminDelay;
    private int _chatRateLimitCount;

    public override void Initialize()
    {
        base.Initialize();

        _periodLength = Configuration.GetCVar(CCVars.ChatRateLimitPeriod);
        _chatRateLimitAnnounceAdmins = Configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdmins);
        _chatRateLimitAnnounceAdminDelay = Configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdminsDelay);
        _chatRateLimitCount = Configuration.GetCVar(CCVars.ChatRateLimitCount);

        Configuration.OnValueChanged(CCVars.ChatRateLimitPeriod, periodLength => _periodLength = periodLength);
        Configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdmins, announce => _chatRateLimitAnnounceAdmins = announce);
        Configuration.OnValueChanged(CCVars.ChatRateLimitAnnounceAdminsDelay, announce => _chatRateLimitAnnounceAdminDelay = announce);
        Configuration.OnValueChanged(CCVars.ChatRateLimitCount, limitCount => _chatRateLimitCount = limitCount);

        InitializeServerDeadChat();
        InitializeServerEmoting();
        InitializeServerLocalChat();
        InitializeServerLoocChat();
        InitializeServerRadio();
        InitializeServerWhisper();
    }

    private string SanitizeSpeechMessage(EntityUid source, string message, out string? emoteStr)
    {
        return SanitizeMessage(source, message, true, out emoteStr);
    }

    private string SanitizeMessage(EntityUid source, string message, bool capitalizeFirstLetter, out string? emoteStr)
    {
        ChatCensor.Censor(message, out message);

        message = message.Trim();
        message = _repAccent.ApplyReplacements(message, "chatsanitize");
        _sanitizer.TrySanitizeOutSmilies(message, source, out message, out emoteStr);

        if (capitalizeFirstLetter)
            message = CapitalizeFirstLetter(message);

        if (UseEnglishGrammar)
            message = CapitalizeIPronoun(message);

        if (ShouldPunctuate)
            message = AddAPeriod(message);

        return message;
    }

    private string SanitizeMessage(string message, bool punctuate = false)
    {
        message = message.Trim();
        ChatCensor.Censor(message, out message);

        if (UseEnglishGrammar)
        {
            message = CapitalizeFirstLetter(message);
            message = CapitalizeIPronoun(message);
        }

        if (punctuate)
            message = AddAPeriod(message);

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

    private string SanitizeName(string name, bool capitalizeFirstLetter)
    {
        var i = name.IndexOf('(') - 1;
        name = i > -2 ? name[..i] : name;

        name = name.Trim();
        ChatCensor.Censor(name, out name);

        if (capitalizeFirstLetter)
            name = CapitalizeFirstLetter(name);

        return FormattedMessage.EscapeText(name);
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
}
