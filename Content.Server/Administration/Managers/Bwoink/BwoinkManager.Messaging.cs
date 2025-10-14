using System.Linq;
using Content.Shared.Administration.Managers.Bwoink;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers.Bwoink;

public sealed partial class ServerBwoinkManager
{
    private void InitializeMessages()
    {
        _netManager.RegisterNetMessage<MsgBwoinkNonAdmin>(BwoinkAttempted);
        _netManager.RegisterNetMessage<MsgBwoink>(AdminBwoinkAttempted);
        _netManager.RegisterNetMessage<MsgBwoinkSyncRequest>(SyncBwoinks);
        _netManager.RegisterNetMessage<MsgBwoinkSync>();

        _netManager.Connected += NetManagerOnConnected;
    }

    private void NetManagerOnConnected(object? _, NetChannelArgs e)
    {
        // A player connected, we send their ahelp history.
        SynchronizeMessages(PlayerManager.GetSessionByChannel(e.Channel));
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
            return;

        // TODO: Logging for when a person can't manage a channel.

        SynchronizeMessage(message.Channel,
            message.Target,
            new BwoinkMessage(message.MsgChannel.UserName,
                message.MsgChannel.UserId,
                DateTime.UtcNow,
                message.Message.Content,
                MessageFlags.Manager));
    }

    private void BwoinkAttempted(MsgBwoinkNonAdmin message)
    {
        if (!IsPrototypeReal(message.Channel))
            return;

        SynchronizeMessage(message.Channel,
            message.MsgChannel.UserId,
            new BwoinkMessage(message.MsgChannel.UserName,
                message.MsgChannel.UserId,
                DateTime.UtcNow,
                message.Message.Content,
                MessageFlags.None));
    }

    /// <summary>
    /// Method that should be invoked whenever a Bwoink is received.
    /// This handles sending the message to the relevant clients with their appropriate packets.
    /// </summary>
    private void SynchronizeMessage(ProtoId<BwoinkChannelPrototype> channel, NetUserId target, BwoinkMessage message)
    {
        InvokeMessageReceived(channel,
            target,
            message.Content,
            message.SenderId,
            message.Sender,
            message.Flags);

        var msgBwoink = new MsgBwoink()
        {
            Channel = channel,
            Message = message,
            Target = target,
        };

        var msgNonAdminBwoink = new MsgBwoinkNonAdmin()
        {
            Message = message,
            Channel = channel,
        };

        var managers = PlayerManager.Sessions.Where(x => CanManageChannel(channel, x))
            .Select(x => x.Channel)
            .ToList();

        _netManager.ServerSendToMany(msgBwoink, managers);

        if (CanManageChannel(channel, PlayerManager.GetSessionById(target)))
            return; // Don't need to send it to the admin client.

        // TODO: Predict on client, so that we don't have to send the message back to the client here.
        _netManager.ServerSendMessage(msgNonAdminBwoink, PlayerManager.GetSessionById(target).Channel);
    }
}
