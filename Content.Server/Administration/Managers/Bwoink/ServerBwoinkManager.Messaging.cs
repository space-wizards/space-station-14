using System.Linq;
using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Features;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.CCVar;
using Content.Shared.Players.RateLimiting;

namespace Content.Server.Administration.Managers.Bwoink;

public sealed partial class ServerBwoinkManager
{
    private void InitializeMessages()
    {
        _netManager.RegisterNetMessage<MsgBwoinkNonAdmin>(BwoinkAttempted);
        _netManager.RegisterNetMessage<MsgBwoink>(AdminBwoinkAttempted);
        _netManager.RegisterNetMessage<MsgBwoinkSyncRequest>(SyncBwoinks);
        _netManager.RegisterNetMessage<MsgBwoinkSync>();
        _netManager.RegisterNetMessage<MsgBwoinkTypingUpdate>(TypingUpdated);
        _netManager.RegisterNetMessage<MsgBwoinkTypings>();
        _netManager.RegisterNetMessage<MsgBwoinkSyncChannelsRequest>(SyncChannelsRequest);
        _netManager.RegisterNetMessage<MsgBwoinkSyncChannels>();

        _netManager.Connected += NetManagerOnConnected;

        _rateLimitManager.Register(
            RateLimitKey,
            new RateLimitRegistration(CCVars.AhelpRateLimitPeriod,
                CCVars.AhelpRateLimitCount,
                null)
        );
    }

    private void TypingUpdated(MsgBwoinkTypingUpdate message)
    {
        if (!IsPrototypeReal(message.Channel))
            return;

        var canManage = CanManageChannel(message.Channel, PlayerManager.GetSessionByChannel(message.MsgChannel));
        if (!canManage && message.ChannelUserId != message.MsgChannel.UserId)
        {
            DebugTools.Assert("Typing set in channel which isn't clients own without manager.");
            Log.Error($"Attempted to set typing for channel without proper permissions! {message.MsgChannel.UserId}!");
            return;
        }

        SetTypingStatus(message.Channel, message.ChannelUserId, message.MsgChannel.UserId, message.IsTyping);
    }

    private void NetManagerOnConnected(object? _, NetChannelArgs e)
    {
        // A player connected, we send their ahelp history.
        var sus = PlayerManager.GetSessionByChannel(e.Channel);
        SynchronizeMessages(sus);
        SyncChannels(sus);
    }

    private void SyncBwoinks(MsgBwoinkSyncRequest message)
    {
        SynchronizeMessages(PlayerManager.GetSessionByChannel(message.MsgChannel));
    }

    private void AdminBwoinkAttempted(MsgBwoink message)
    {
        if (!IsPrototypeReal(message.Channel))
            return;

        if (!CanManageChannel(message.Channel, PlayerManager.GetSessionByChannel(message.MsgChannel)))
        {
            Log.Error($"Attempted admin bwoink without proper perms {message.Channel} {message.MsgChannel.UserId}");
            return;
        }

        var flags = MessageFlags.Manager;

        // Validating message flags.
        if (ProtoCache[message.Channel].HasFeature<ManagerOnlyMessages>() &&
            message.Message.Flags.HasFlag(MessageFlags.ManagerOnly))
        {
            flags |= MessageFlags.ManagerOnly;
        }

        if (ProtoCache[message.Channel].TryGetFeature<SoundOnMessage>(out var soundOnMessage) &&
            message.Message.Flags.HasFlag(MessageFlags.Silent) && soundOnMessage.AllowSilent)
        {
            flags |= MessageFlags.Silent;
        }

        var gameTickerNonsense = GetRoundIdAndTime();

        SynchronizeMessage(message.Channel,
            message.Target,
            new BwoinkMessage(message.MsgChannel.UserName,
                message.MsgChannel.UserId,
                DateTime.UtcNow,
                message.Message.Content,
                flags,
                gameTickerNonsense.roundTime,
                gameTickerNonsense.roundId));
    }

    private void BwoinkAttempted(MsgBwoinkNonAdmin message)
    {
        if (!IsPrototypeReal(message.Channel))
            return;

        if (!CanWriteChannel(message.Channel, PlayerManager.GetSessionByChannel(message.MsgChannel)))
        {
            Log.Error($"Attempted bwoink without proper perms {message.Channel} {message.MsgChannel.UserId}");
            return;
        }

        var gameTickerNonsense = GetRoundIdAndTime();

        SynchronizeMessage(message.Channel,
            message.MsgChannel.UserId,
            new BwoinkMessage(message.MsgChannel.UserName,
                message.MsgChannel.UserId,
                DateTime.UtcNow,
                message.Message.Content,
                MessageFlags.None,
                gameTickerNonsense.roundTime,
                gameTickerNonsense.roundId));
    }

    /// <summary>
    /// Method that should be invoked whenever a Bwoink is received.
    /// This handles sending the message to the relevant clients with their appropriate packets.
    /// </summary>
    private void SynchronizeMessage(ProtoId<BwoinkChannelPrototype> channel, NetUserId target, BwoinkMessage message)
    {
        PlayerManager.TryGetSessionById(target, out var targetSes);
        var senderSes = message.SenderId.HasValue ? PlayerManager.GetSessionById(message.SenderId.Value) : null;

        // Since the sender session can be null, we default to allowed and then check if there only is a session.
        var gotRateLimited = RateLimitStatus.Allowed;
        if (senderSes != null)
        {
            gotRateLimited = _rateLimitManager.CountAction(senderSes, RateLimitKey);
        }

        if (gotRateLimited == RateLimitStatus.Blocked)
        {
            var rateLimitMessage = CreateSystemMessage(LocalizationManager.GetString("bwoink-system-rate-limited"),
                MessageFlags.Silent | MessageFlags.Manager | MessageFlags.System);

            var rateLimitMessageMsg = new MsgBwoinkNonAdmin()
            {
                Message = rateLimitMessage,
                Channel = channel,
            };

            // ReSharper disable once NullableWarningSuppressionIsUsed
            // Assuming no cosmic ray hits us, we will only ever reach this path if senderSes is not null
            _netManager.ServerSendMessage(rateLimitMessageMsg, senderSes!.Channel);
            return;
        }

        var managers = PlayerManager.Sessions.Where(x => CanManageChannel(channel, x))
            .Select(x => x.Channel)
            .ToList();

        if (managers.Count == 0)
        {
            message.Flags |= MessageFlags.NoReceivers;
        }

        InvokeMessageReceived(channel, target, message);

        var msgBwoink = new MsgBwoink()
        {
            Channel = channel,
            Message = message,
            Target = target,
        };

        _netManager.ServerSendToMany(msgBwoink, managers);

        if (message.Flags.HasFlag(MessageFlags.ManagerOnly) || targetSes == null)
            return; // Stop here.

        if (CanManageChannel(channel, targetSes))
            return; // Don't need to send it to the admin client.

        if (!CanReadChannel(channel, targetSes))
        {
            // Target can't read it. Womp. Womp.
            var notificationMessage = CreateSystemMessage(LocalizationManager.GetString("bwoink-channel-no-readers"), MessageFlags.Manager | MessageFlags.ManagerOnly);
            SynchronizeMessage(channel, target, notificationMessage);
            return;
        }

        var eventArgs = new BwoinkMessageClientSentEventArgs(message with { }, targetSes);
        MessageBeingSent?.Invoke(eventArgs);

        var msgNonAdminBwoink = new MsgBwoinkNonAdmin()
        {
            Message = eventArgs.Message,
            Channel = channel,
        };

        _netManager.ServerSendMessage(msgNonAdminBwoink, targetSes.Channel);
    }
}
