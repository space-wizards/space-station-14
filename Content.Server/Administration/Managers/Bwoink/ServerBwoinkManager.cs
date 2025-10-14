using Content.Shared.Administration.Managers.Bwoink;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers.Bwoink;

public sealed partial class ServerBwoinkManager : SharedBwoinkManager
{
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeMessages();
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    /// <summary>
    /// Validates that the protoId given is actually a real prototype. Needed since clients can just send whatever as the ID.
    /// </summary>
    private bool IsPrototypeReal(ProtoId<BwoinkChannelPrototype> channel)
    {
        // If this fails, Resolve will log an error.
        return PrototypeManager.Resolve<BwoinkChannelPrototype>(channel, out _);
    }

    /// <summary>
    /// Re-sends the conversations for a session. This respects manager permissions and such.
    /// </summary>
    public void SynchronizeMessages(ICommonSession session)
    {
        var conversations = new Dictionary<ProtoId<BwoinkChannelPrototype>, Dictionary<NetUserId, Conversation>>();

        foreach (var (channelId, channel) in ProtoCache)
        {
            if (CanManageChannel(channel, session))
            {
                // This person is a manager, so we send them all the conversations for this channel.

                conversations.Add(channelId, GetConversationsForChannel(channelId));
            }
            else
            {
                // Not a manager, you get 1984'd messages that are only related to you.

                var convo = GetFilteredConversation(session.UserId, channelId, true);
                conversations.Add(channelId,
                    convo != null
                        ? new Dictionary<NetUserId, Conversation> { { session.UserId, convo } }
                        : []);
            }
        }

        _netManager.ServerSendMessage(new MsgBwoinkSync()
        {
            Conversations = conversations
        }, session.Channel);
    }
}
