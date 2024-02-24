using System.Globalization;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem
{
    public bool TrySendWhisperMessage(EntityUid entityUid, string message, string asName = "")
    {
        if (!TryComp<WhisperableComponent>(entityUid, out var whisper))
            return false;

        SendWhisperMessage(entityUid, message, whisper.MinRange, whisper.MaxRange, asName);

        return true;
    }

    /// <summary>
    /// Send a whisper.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="minRange">The maximum range the audio can be fully heard in</param>
    /// <param name="maxRange">The maximum range the audio can be heard at all in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    public void SendWhisperMessage(EntityUid entityUid, string message, float minRange, float maxRange, string asName = "")
    {
        // You can't whisper if you're shouting and are capable of talking normally.
        if (IsShouting(message) && TrySendLocalChatMessage(entityUid, message, asName))
        {
            return;
        }

        if (!TrySanitizeAndTransformSpokenMessage(entityUid, ref message, ref asName, out var name))
            return;

        var obfuscatedMessage = ObfuscateMessageReadability(message, 0.2f);

        RaiseLocalEvent(new WhisperEmittedEvent(entityUid, name, minRange, maxRange, message, obfuscatedMessage));

        var msgOut = new WhisperEvent(
            GetNetEntity(entityUid),
            name,
            message
        );

        var obfuscatedMsgOut = new WhisperEvent(
            GetNetEntity(entityUid),
            name,
            obfuscatedMessage
        );

        var totallyObfuscatedlyMsgOut = new WhisperEvent(
            GetNetEntity(entityUid),
            "",
            obfuscatedMessage
        );

        var recipients = GetWhisperRecipients(entityUid, minRange, maxRange);

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

        _replay.RecordServerMessage(msgOut);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {ToPrettyString(entityUid):user} as {asName}: {message}");
    }

    /// <summary>
    /// Get the recipients who can hear this whisper.
    /// </summary>
    /// <param name="source">The whisperer.</param>
    /// <param name="minRange">The maximum range that the entire whisper can be heard.</param>
    /// <param name="maxRange">The maximum range the whisper can be heard at all.</param>
    /// <returns>Returns a tuple of those in the minimum range (zeroth) and the maximum range but not the minimum range (first).</returns>
    private (List<ICommonSession> insideMinRange, List<ICommonSession> insideMaxRangeExclusiveMin, List<ICommonSession> insideMaxRangeExclusiveMinNoSight) GetWhisperRecipients(EntityUid source, float minRange, float maxRange)
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
            var distance = float.MaxValue;

            // even if they are a ghost hearer, in some situations we still need the range
            if (!ghostHearing.HasComponent(playerEntity) &&
                (!sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out distance) ||
                 !(distance < maxRange)))
                continue;

            if (distance < minRange)
                minRecipients.Add(player);
            else if(_interactionSystem.InRangeUnobstructed(source, playerEntity, maxRange, Shared.Physics.CollisionGroup.Opaque))
                maxRecipients.Add(player);
            else
                maxRecipientsNoSight.Add(player);
        }

        return (minRecipients, maxRecipients, maxRecipientsNoSight);
    }

    /// <summary>
    /// Returns if we think someone is talking loudly enough to not be whispering. The rules are:
    /// 1. The message ends with two exclamation marks (as it's possible to exclaim whilst whispering)
    /// 2. The message is entirely all caps (because "SCIENCE IS SO UTTERLY WORTHLESS" is clearly shouting) and is long enough not to clash with department heads (e.g. "CMO.").
    /// </summary>
    private bool IsShouting(string message)
    {
        if (AllowShoutWhispers)
        {
            return false;
        }

        if (message.EndsWith("!!"))
        {
            return true;
        }

        if (!UpperCaseMessagesMeanShouting)
        {
            return false;
        }

        return message.Length >= 5 && message.Equals(message.ToUpper(), StringComparison.CurrentCulture);
    }
}
