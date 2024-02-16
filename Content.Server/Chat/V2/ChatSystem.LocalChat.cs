using System.Globalization;
using System.Linq;
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
using SixLabors.ImageSharp.Processing;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem
{
    public void InitializeLocalChat()
    {
        // A client attempts to chat using a given entity
        SubscribeNetworkEvent<AttemptLocalChatEvent>((msg, args) => { HandleAttemptLocalChatMessage(args.SenderSession, msg.Speaker, msg.Message); });
    }

    private void HandleAttemptLocalChatMessage(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);
        if (player.AttachedEntity != entityUid)
        {
            return;
        }

        if (IsRateLimited(entityUid, out var reason))
        {
            RaiseNetworkEvent(new LocalChatFailedEvent(entity, reason), player);

            return;
        }

        if (!TryComp<LocalChattableComponent>(entityUid, out var comp))
        {
            RaiseNetworkEvent(new LocalChatFailedEvent(entity, Loc.GetString("chat-system-local-chat-failed")), player);

            return;
        }

        if (message.Length > MaxChatMessageLength)
        {
            RaiseNetworkEvent(new LocalChatFailedEvent(entity, Loc.GetString("chat-system-max-message-length-exceeded-message")), player);

            return;
        }

        SendLocalChatMessage(entityUid, message, comp.Range);
    }

    /// <summary>
    /// Try to end a chat in Local.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    public bool TrySendLocalChatMessage(EntityUid entityUid, string message, string asName = "")
    {
        if (!TryComp<LocalChattableComponent>(entityUid, out var chat))
            return false;

        SendLocalChatMessage(entityUid, message, chat.Range, asName);

        return true;
    }

    /// <summary>
    /// Send a chat in Local.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="range">The range the audio can be heard in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    public void SendLocalChatMessage(EntityUid entityUid, string message, float range, string asName = "")
    {
        message = SanitizeInCharacterMessage(entityUid,message,out var emoteStr);

        if (emoteStr?.Length > 0)
        {
            TrySendEmoteMessageWithoutRecursion(entityUid, emoteStr, asName);
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

        var name = FormattedMessage.EscapeText(asName);
        RaiseLocalEvent(entityUid, new LocalChatSuccessEvent(
            GetNetEntity(entityUid),
            name,
            message,
            range
        ), true);

        var msgOut = new LocalChatEvent(
            GetNetEntity(entityUid),
            name,
            message
        );

        foreach (var session in GetLocalChatRecipients(entityUid, range))
        {
            RaiseNetworkEvent(msgOut, session);
        }

        _replay.RecordServerMessage(msgOut);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Local chat from {ToPrettyString(entityUid):user} as {asName}: {message}");
    }

    public void SendSubtleChatMessage(ICommonSession source, ICommonSession target, string message)
    {
        var msgOut = new SubtleChatEvent(GetNetEntity(EntityUid.Invalid),message);

        RaiseNetworkEvent(msgOut, target);

        _adminLogger.Add(LogType.AdminMessage, LogImpact.Low, $"{ToPrettyString(target.AttachedEntity):player} received subtle message from {source.Name}: {message}");
    }

    public void SendBackgroundChatMessage(EntityUid source, string message, string asName = "")
    {
        RaiseNetworkEvent(new BackgroundChatEvent(GetNetEntity(EntityUid.Invalid), message, asName));
    }

    private List<ICommonSession> GetLocalChatRecipients(EntityUid source, float range)
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

/// <summary>
/// A server-only event that is fired when an entity chats in local chat.
/// </summary>
[Serializable]
public sealed class LocalChatSuccessEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public float Range;

    public LocalChatSuccessEvent(NetEntity speaker, string asName, string message, float range)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        Range = range;
    }
}
