using System.Globalization;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed class ServerWhisperSystem : EntitySystem
{
    [Dependency] private readonly ServerEmoteSystem _emote = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
    [Dependency] private readonly ReplacementAccentSystem _repAccent = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ILogManager _logger = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly IChatRateLimiter _rateLimiter = default!;
    [Dependency] private readonly IChatUtilities _chatUtilities = default!;

    public override void Initialize()
    {
        base.Initialize();

        // A client attempts to whisper using a given entity
        SubscribeNetworkEvent<WhisperAttemptedEvent>((msg, args) => { HandleAttemptWhisperEvent(args.SenderSession, msg.Speaker, msg.Message); });
    }

    private void HandleAttemptWhisperEvent(ICommonSession player, NetEntity entity, string message)
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
        if (!TryComp<WhisperableComponent>(entityUid, out var whisperable))
        {
            RaiseNetworkEvent(new WhisperAttemptFailedEvent(entity, "You can't whisper"), player);

            return;
        }

        var maxMessageLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        // Is the message too long?
        if (message.Length > _configurationManager.GetCVar(CCVars.ChatMaxMessageLength))
        {
            RaiseNetworkEvent(
                new WhisperAttemptFailedEvent(
                    entity,
                    Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", maxMessageLen))
                    ),
                player);

            return;
        }

        // All good; let's actually send a chat message.
        SendWhisperMessage(entityUid, message, whisperable.MinRange, whisperable.MaxRange);
    }

    public bool TrySendWhisperMessage(EntityUid entityUid, string message, string asName = "")
    {
        if (!TryComp<WhisperableComponent>(entityUid, out var whisper))
            return false;

        SendWhisperMessage(entityUid, message, whisper.MinRange, whisper.MaxRange, asName);

        return true;

    }

    /// <summary>
    /// Send a chat in Local.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="minRange">The maximum range the audio can be fully heard in</param>
    /// <param name="maxRange">The maximum range the audio can be heard at all in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    public void SendWhisperMessage(EntityUid entityUid, string message, float minRange, float maxRange, string asName = "")
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

        // TODO: figure out if this goes BEFORE or AFTER which step in transformation...
        var obfuscatedMessage = _chatUtilities.ObfuscateMessageReadability(message, 0.2f);

        var verb = _chatUtilities.GetSpeechVerb(entityUid, message);

        var name = FormattedMessage.EscapeText(asName);

        var nameColor = "";

        // color the name unless it's something like "the old man"
        if (!TryComp<GrammarComponent>(entityUid, out var grammar) || grammar.ProperNoun == true)
            nameColor = _chatUtilities.GetNameColor(name);

        var msgOut = new EntityWhisperedEvent(
            GetNetEntity(entityUid),
            name,
            verb.FontId,
            verb.FontSize,
            verb.Bold,
            nameColor,
            minRange,
            message
        );

        var obfuscatedMsgOut = new EntityWhisperedObfuscatedlyEvent(
            GetNetEntity(entityUid),
            name,
            verb.FontId,
            verb.FontSize,
            verb.Bold,
            nameColor,
            maxRange,
            obfuscatedMessage
        );

        var totallyObfuscatedlyMsgOut = new EntityWhisperedTotallyObfuscatedlyEvent(
            GetNetEntity(entityUid),
            verb.FontId,
            verb.FontSize,
            verb.Bold,
            maxRange,
            obfuscatedMessage
        );

        // Make sure anything server-side hears about the message
        // TODO: what does broadcasting even do
        RaiseLocalEvent(entityUid, msgOut, true);
        RaiseLocalEvent(entityUid, obfuscatedMsgOut, true);
        RaiseLocalEvent(entityUid, totallyObfuscatedlyMsgOut, true);

        var recipients = GetRecipients(entityUid, minRange, maxRange);

        // Now fire it off to legal recipients
        foreach (var session in recipients.insideMinRange)
        {
            RaiseNetworkEvent(msgOut, session);
        }

        foreach (var session in recipients.insideMaxRangeExclusiveMin)
        {
            RaiseNetworkEvent(obfuscatedMsgOut, session);
        }

        foreach (var session in recipients.insideMaxRangeExclusiveMinNoSight)
        {
            RaiseNetworkEvent(totallyObfuscatedlyMsgOut, session);
        }

        // And finally, stash it in the replay and log.
        _replay.RecordServerMessage(msgOut);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {ToPrettyString(entityUid):user} as {asName}: {message}");
    }

    // TODO: All this sanitization code should be migrated to an implementation of IChatSanitizationManager.
    private string SanitizeInGameICMessage(EntityUid source, string message, out string? emoteStr, bool punctuate = false, bool capitalizeTheWordI = true)
    {
        message = message.Trim();
        // TODO: This "chatsanitize" magic string is required because sanitization is messily handled; migrate this
        // code into the IChatSanitizationManager impl.
        message = _repAccent.ApplyReplacements(message, "chatsanitize");
        message = _chatUtilities.CapitalizeFirstLetter(message);

        if (capitalizeTheWordI)
            message = _chatUtilities.CapitalizeIPronoun(message);

        if (punctuate)
            message = _chatUtilities.AddAPeriod(message);

        // Wait if we have a sanitizer why the fuck is this code not in it
        // God is dead

        _sanitizer.TrySanitizeOutSmilies(message, source, out message, out emoteStr);

        return message;
    }

    private bool IsRateLimited(EntityUid entityUid)
    {
        if (!_rateLimiter.IsRateLimited(entityUid, out var reason))
            return false;

        if (!string.IsNullOrEmpty(reason))
        {
            RaiseNetworkEvent(
                new WhisperAttemptFailedEvent(
                    GetNetEntity(entityUid),
                    Loc.GetString(Loc.GetString("chat-manager-rate-limited"))
                ),
                entityUid
            );
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

    /// <summary>
    /// Get the recipients who can hear this whisper.
    /// </summary>
    /// <param name="source">The whisperer.</param>
    /// <param name="minRange">The maximum range that the entire whisper can be heard.</param>
    /// <param name="maxRange">The maximum range the whisper can be heard at all.</param>
    /// <returns>Returns a tuple of those in the minimum range (zeroth) and the maximum range but not the minimum range (first).</returns>
    private (List<ICommonSession> insideMinRange, List<ICommonSession> insideMaxRangeExclusiveMin, List<ICommonSession> insideMaxRangeExclusiveMinNoSight) GetRecipients(EntityUid source, float minRange, float maxRange)
    {
        var minRecipients = new List<ICommonSession>();
        var maxRecipients = new List<ICommonSession>();
        var maxRecipientsNoSight = new List<ICommonSession>();

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

            // Use MaxValue here to stop any shenanigans where it being defaulted to 0.0 causes it to always be in range
            float distance = float.MaxValue;

            // even if they are a ghost hearer, in some situations we still need the range
            if (ghostHearing.HasComponent(playerEntity) ||
                sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out distance) &&
                distance < maxRange
            )
            {
                if (distance < minRange)
                {
                    minRecipients.Add(player);
                }
                else if(_interactionSystem.InRangeUnobstructed(source, playerEntity, maxRange, Shared.Physics.CollisionGroup.Opaque))
                {
                    maxRecipients.Add(player);
                }
                else
                {
                    maxRecipientsNoSight.Add(player);
                }
            }
        }

        return (minRecipients, maxRecipients, maxRecipientsNoSight);
    }
}
