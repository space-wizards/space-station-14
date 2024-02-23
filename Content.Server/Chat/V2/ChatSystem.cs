using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Chat.V2.Moderation;
using Content.Server.Speech.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.VoiceMask;
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

    public override void Initialize()
    {
        base.Initialize();

        Configuration.OnValueChanged(CCVars.LoocEnabled, OnLoocEnabledChanged, true);
        Configuration.OnValueChanged(CCVars.DeadLoocEnabled, OnDeadLoocEnabledChanged, true);
        Configuration.OnValueChanged(CCVars.CritLoocEnabled, OnCritLoocEnabledChanged, true);

        SubscribeLocalEvent<DeadChatCreatedEvent>(msg => { SendDeadChatMessage(msg.Speaker, msg.Message); });
        SubscribeLocalEvent<EmoteCreatedEvent>(msg => { SendEmoteMessage(msg.Sender, msg.Message, msg.Range); });
        SubscribeLocalEvent<LocalChatCreatedEvent>(msg => { SendLocalChatMessage(msg.Speaker, msg.Message, msg.Range); });
        SubscribeLocalEvent<LoocCreatedEvent>(msg => { SendLoocMessage(msg.Speaker, msg.Message); });
        SubscribeLocalEvent<RadioCreatedEvent>(msg => { SendRadioMessageWithSpeech(msg.Speaker, msg.Message, msg.Channel); });
        SubscribeLocalEvent<WhisperCreatedEvent>(msg => { SendWhisperMessage(msg.Speaker, msg.Message, msg.MinRange, msg.MaxRange); });
    }

    private bool TrySanitizeAndTransformSpokenMessage(EntityUid entityUid, ref string message, ref string asName, out string name)
    {
        name = "";
        message = SanitizeSpeechMessage(entityUid, message, out var emoteStr);

        if (emoteStr?.Length > 0)
        {
            TrySendEmoteMessageWithoutRecursion(entityUid, emoteStr, asName);
        }

        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        // Mitigation for exceptions such as https://github.com/space-wizards/space-station-14/issues/24671
        try
        {
            message = FormattedMessage.RemoveMarkup(message);
        }
        catch (Exception e)
        {
            _logger.GetSawmill("chat")
                .Error(
                    $"UID {entityUid} attempted to send {message} {(asName.Length > 0 ? "as name, " : "")} but threw a parsing error: {e}");

            return false;
        }

        message = TransformSpeech(entityUid, FormattedMessage.RemoveMarkup(message));

        if (string.IsNullOrEmpty(message))
            return false;

        if (string.IsNullOrEmpty(asName))
            asName = GetSpeakerName(entityUid);

        if (TryComp<VoiceMaskComponent>(entityUid, out var mask))
        {
            asName = mask.VoiceName;
        }

        name = SanitizeName(asName, CurrentCultureIsSomeFormOfEnglish);

        return true;
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

        if (CurrentCultureIsSomeFormOfEnglish)
            message = CapitalizeIPronoun(message);

        if (ShouldPunctuate)
            message = AddAPeriod(message);

        return message;
    }

    private string SanitizeMessage(string message, bool punctuate = false)
    {
        message = message.Trim();
        ChatCensor.Censor(message, out message);

        if (CurrentCultureIsSomeFormOfEnglish)
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
}
