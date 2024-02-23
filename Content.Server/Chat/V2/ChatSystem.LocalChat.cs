using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem
{
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
        if (!TrySanitizeAndTransformSpokenMessage(entityUid, ref message, ref asName, out var name))
            return;

        var msgOut = new LocalChatEvent(GetNetEntity(entityUid), name, message);

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
        RaiseNetworkEvent(new BackgroundChatEvent(GetNetEntity(EntityUid.Invalid), message, SanitizeName(asName, CurrentCultureIsSomeFormOfEnglish)));
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

