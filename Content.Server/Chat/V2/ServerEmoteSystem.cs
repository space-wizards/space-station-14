using System.Globalization;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed class ServerEmoteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _logger = default!;
    [Dependency] private readonly IEmoteConfigManager _emoteConfig = default!;
    [Dependency] private readonly IChatRateLimiter _rateLimiter = default!;
    [Dependency] private readonly IChatUtilities _chatUtilities = default!;

    public override void Initialize()
    {
        base.Initialize();

        // A client attempts to chat using a given entity
        SubscribeNetworkEvent<EmoteAttemptedEvent>((msg, args) => { HandleAttemptEmoteMessage(args.SenderSession, msg.Emoter, msg.Message); });
    }

    private void HandleAttemptEmoteMessage(ICommonSession player, NetEntity entity, string message)
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
        if (!TryComp<EmoteableComponent>(entityUid, out var emoteable))
        {
            RaiseNetworkEvent(new EmoteAttemptFailedEvent(entity, "You can't emote"), player);

            return;
        }

        var maxMessageLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        // Is the message too long?
        if (message.Length > _configurationManager.GetCVar(CCVars.ChatMaxMessageLength))
        {
            RaiseNetworkEvent(
                new EmoteAttemptFailedEvent(
                    entity,
                    Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", maxMessageLen))
                    ),
                player);

            return;
        }

        // All good; let's actually send a chat message.
        SendEmoteMessage(entityUid, message, emoteable.Range);
    }

    public bool TrySendEmoteMessage(EntityUid entityUid, string message, string asName = "", bool isRecursive = false)
    {
        if (!TryComp<EmoteableComponent>(entityUid, out var emote))
            return false;

        SendEmoteMessage(entityUid, message, emote.Range, asName, isRecursive);

        return true;
    }

    /// <summary>
    /// Emote.
    /// </summary>
    /// <param name="entityUid">The entity who is emoting</param>
    /// <param name="message">The message to send.</param>
    /// <param name="range">The range the emote can be seen at</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    /// <param name="isRecursive">If this emote is being sent because of another message. Prevents multiple emotes being sent for the same input.</param>
    public void SendEmoteMessage(EntityUid entityUid, string message, float range, string asName = "", bool isRecursive = false)
    {
        // Capitalizing I still happens for emotes in English for correctness of grammar, even though emotes are
        // written in the third person in English.

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

        if (!string.IsNullOrEmpty(emoteStr))
        {
            // If they wrote something like '@dances really badly lol` then this converts to `Urist dances really badly` and `Urist laughs`.
            // We trim this to only allowing one recursion; this prevents an abusive message like `lol lol lol lol lol lol lol lol lol lol`
            SendEmoteMessage(entityUid, emoteStr, range, asName, true);
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

        message = FormattedMessage.RemoveMarkup(message);

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (string.IsNullOrEmpty(asName))
        {
            asName = Name(entityUid);
        }

        var name = FormattedMessage.EscapeText(asName);

        var emote = _emoteConfig.GetEmote(message);
        if (emote != null)
        {
            // EmoteEvent is used by a number of other systems to do things. We can't easily migrate and encapsulate that work here.
            // Specifically, some systems for Cluwnes, Zombies etc rely on it to allow sounds to be emitted for emotes.
            var ev = new EmoteEvent(emote);
            RaiseLocalEvent(entityUid, ref ev);
        }

        var msgOut = new EntityEmotedEvent(
            GetNetEntity(entityUid),
            name,
            message,
            range
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

    private string SanitizeInGameICMessage(EntityUid source, string message, out string? emoteStr, bool punctuate = false, bool capitalizeTheWordI = true)
    {
        message = message.Trim();

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
            RaiseNetworkEvent(
                new LocalChatAttemptFailedEvent(
                    GetNetEntity(entityUid),
                    Loc.GetString(Loc.GetString("chat-manager-rate-limited"))
                ),
                entityUid
            );
        }

        return true;
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

    public void TryEmoteWithChat(EntityUid source, string emoteId, string nameOverride = "")
    {
        if (!TryComp<EmoteableComponent>(source, out var comp))
            return;

        if (!_prototypeManager.TryIndex<EmotePrototype>(emoteId, out var emote))
            return;

        // check if proto has valid message for chat
        if (emote.ChatMessages.Count != 0)
        {
            var action = Loc.GetString(_random.Pick(emote.ChatMessages), ("entity", source));
            SendEmoteMessage(source, action, comp.Range, nameOverride);
        }
        else
        {
            // do the rest of emote event logic here
            TryEmoteWithoutChat(source, emoteId);
        }
    }

    public void TryEmoteWithoutChat(EntityUid source, string emoteId)
    {
        if (!TryComp<EmoteableComponent>(source, out var comp))
            return;

        if (!_prototypeManager.TryIndex<EmotePrototype>(emoteId, out var emote))
            return;

        // EmoteEvent is used by a number of other systems to do things. We can't easily migrate and encapsulate that work here.
        // Specifically, some systems for Cluwnes, Zombies etc rely on it to allow sounds to be emitted for emotes.
        var ev = new EmoteEvent(emote);
        RaiseLocalEvent(source, ref ev);
    }
}
