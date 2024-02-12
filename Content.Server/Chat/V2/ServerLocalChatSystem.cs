using System.Globalization;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Chat.V2.Censorship;
using Content.Server.Speech.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed class ServerLocalChatSystem : EntitySystem
{
    [Dependency] private readonly ServerEmoteSystem _emote = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
    [Dependency] private readonly ReplacementAccentSystem _repAccent = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _logger = default!;
    [Dependency] private readonly IChatRateLimiter _rateLimiter = default!;
    [Dependency] private readonly IChatUtilities _chatUtilities = default!;

    public override void Initialize()
    {
        base.Initialize();

        // A client attempts to chat using a given entity
        SubscribeNetworkEvent<LocalChatAttemptedEvent>((msg, args) => { HandleAttemptChatMessage(args.SenderSession, msg.Speaker, msg.Message); });
    }

    private void HandleAttemptChatMessage(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);

        if (player.AttachedEntity != entityUid)
        {
            // Nice try bozo.
            return;
        }

        // Are they rate-limited
        if (IsRateLimited(entityUid))
        {
            return;
        }

        // Sanity check: if you can't chat you shouldn't be chatting.
        if (!TryComp<LocalChattableComponent>(entityUid, out var chattable))
        {
            RaiseNetworkEvent(new LocalChatAttemptFailedEvent(entity, "You can't chat"), player);

            return;
        }

        var maxMessageLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        // Is the message too long?
        if (message.Length > _configurationManager.GetCVar(CCVars.ChatMaxMessageLength))
        {
            RaiseNetworkEvent(
                new LocalChatAttemptFailedEvent(
                    entity,
                    Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", maxMessageLen))
                    ),
                player);

            return;
        }

        // All good; let's actually send a chat message.
        SendLocalChatMessage(entityUid, message, chattable.Range);
    }

    public bool TrySendLocalChatMessage(EntityUid entityUid, string message, string asName = "", bool hideInChatLog = false)
    {
        if (!TryComp<LocalChattableComponent>(entityUid, out var chat))
            return false;

        SendLocalChatMessage(entityUid, message, chat.Range, asName, hideInChatLog);

        return true;
    }

    /// <summary>
    /// Send a chat in Local.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="range">The range the audio can be heard in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    public void SendLocalChatMessage(EntityUid entityUid, string message, float range, string asName = "", bool hideInLog = false)
    {
        // TODO: formatting for languages should be its own code that can be called via a manager!
        var shouldCapitalizeTheWordI = (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
                                       || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en");

        message = SanitizeInGameICMessage(
            entityUid,
            message,
            out var emoteStr,
            _configuration.GetCVar(CCVars.ChatPunctuation),
            shouldCapitalizeTheWordI
        );

        if (emoteStr?.Length > 0)
        {
            _emote.TrySendEmoteMessage(entityUid, emoteStr, asName, true);
        }

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        // Mitigation for exceptions such as https://github.com/space-wizards/space-station-14/issues/24671
        try
        {
            message = FormattedMessage.RemoveMarkup(message);
        }
        catch (Exception e)
        {
            _logger.GetSawmill("chat").Error($"UID {entityUid} attempted to send {message} {(asName.Length > 0 ? "as name, " : "")} but threw a parsing error: {e}");

            return;
        }

        message = TransformSpeech(entityUid, FormattedMessage.RemoveMarkup(message));

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (string.IsNullOrEmpty(asName))
        {
            asName = GetSpeakerName(entityUid);
        }

        var verb = _chatUtilities.GetSpeechVerb(entityUid, message);

        var name = FormattedMessage.EscapeText(asName);

        var nameColor = "";

        // color the name unless it's something like "the old man"
        if (!TryComp<GrammarComponent>(entityUid, out var grammar) || grammar.ProperNoun == true)
            nameColor = _chatUtilities.GetNameColor(name);

        var msgOut = new EntityLocalChattedEvent(
            GetNetEntity(entityUid),
            name,
            Loc.GetString(_random.Pick(verb.SpeechVerbStrings)),
            verb.FontId,
            verb.FontSize,
            verb.Bold,
            nameColor,
            message,
            range,
            hideInLog
        );

        // Make sure anything server-side hears about the message
        // TODO: what does broadcasting even do
        RaiseLocalEvent(entityUid, msgOut, true);

        // Now fire it off to legal recipients
        foreach (var session in GetRecipients(entityUid, range))
        {
            RaiseNetworkEvent(msgOut, session);
        }

        // And finally, stash it in the replay and log.
        _replay.RecordServerMessage(msgOut);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {ToPrettyString(entityUid):user} as {asName}: {message}");
    }

    public void SendSubtleLocalChatMessage(ICommonSession source, ICommonSession target, string message)
    {
        // Use any ol' verb here.
        var verb = _chatUtilities.GetSpeechVerb(EntityUid.Invalid, message);

        var msgOut = new EntityLocalChattedEvent(
            GetNetEntity(EntityUid.Invalid),
            "",
            "",
            verb.FontId,
            verb.FontSize,
            verb.Bold,
            "",
            message,
            0,
            false,
            isSubtle:true
        );

        RaiseNetworkEvent(msgOut, target);

        _adminLogger.Add(LogType.AdminMessage, LogImpact.Low, $"{ToPrettyString(target.AttachedEntity):player} received subtle message from {source.Name}: {message}");
    }
    private string SanitizeInGameICMessage(EntityUid source, string message, out string? emoteStr, bool punctuate = false, bool capitalizeTheWordI = true)
    {
        message = message.Trim();
        ChatCensor.Censor(message, out message);
        message = _repAccent.ApplyReplacements(message, "chatsanitize");
        message = _chatUtilities.CapitalizeFirstLetter(message);

        if (capitalizeTheWordI)
            message = _chatUtilities.CapitalizeIPronoun(message);

        if (punctuate)
            message = _chatUtilities.AddAPeriod(message);

        _sanitizer.TrySanitizeOutSmilies(message, source, out message, out emoteStr);

        return message;
    }

    private bool IsRateLimited(EntityUid entityUid)
    {
        if (!_rateLimiter.IsRateLimited(entityUid, out var reason))
            return false;

        if (!string.IsNullOrEmpty(reason))
        {
            RaiseNetworkEvent(new LocalChatAttemptFailedEvent(GetNetEntity(entityUid),reason), entityUid);
        }

        return true;
    }

    private string TransformSpeech(EntityUid sender, string message)
    {
        // TODO: This is to do with the accent system as it currently exists. We're not refactoring accents at the
        // moment but this will need to be changed when this is looked into.
        var ev = new TransformSpeechEvent(sender, message);
        RaiseLocalEvent(ev);

        return ev.Message;
    }

    private string GetSpeakerName(EntityUid entityToBeNamed)
    {
        // More wonkiness with raised local events...
        var nameEv = new TransformSpeakerNameEvent(entityToBeNamed, Name(entityToBeNamed));
        RaiseLocalEvent(entityToBeNamed, nameEv);

        return nameEv.Name;
    }

    private List<ICommonSession> GetRecipients(EntityUid source, float range)
    {
        var recipients = new List<ICommonSession>();

        var ghostHearing = GetEntityQuery<GhostHearingComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var transformSource = xforms.GetComponent(source);
        var sourceMapId = transformSource.MapID;
        var sourceCoords = transformSource.Coordinates;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceMapId)
                continue;

            // even if they are a ghost hearer, in some situations we still need the range
            if (ghostHearing.HasComponent(playerEntity) || sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance) && distance < range)
                recipients.Add(player);
        }

        return recipients;
    }
}
