using System.Linq;
using Content.Server.GameTicking;
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

        if (CanManageChannel(channel, PlayerManager.GetSessionById(target)))
            return; // Don't need to send it to the admin client.

        _netManager.ServerSendToMany(msgBwoink, managers);

        // TODO: Predict on client, so that we don't have to send the message back to the client here.
        _netManager.ServerSendMessage(msgNonAdminBwoink, PlayerManager.GetSessionById(target).Channel);
    }
}

public sealed class AHelpMessage(
    string username,
    string message,
    bool isAdmin,
    string roundTime,
    GameRunLevel roundState,
    bool playedSound,
    bool adminOnly = false,
    bool noReceivers = false,
    string? icon = null)
{
    public string Username { get; set; } = username;
    public string Message { get; set; } = message;
    public bool IsAdmin { get; set; } = isAdmin;
    public string RoundTime { get; set; } = roundTime;
    public GameRunLevel RoundState { get; set; } = roundState;
    public bool PlayedSound { get; set; } = playedSound;
    public readonly bool AdminOnly = adminOnly;
    public bool NoReceivers { get; set; } = noReceivers;
    public string? Icon { get; set; } = icon;
}
