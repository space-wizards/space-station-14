using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Voting;

public sealed class MsgVoteMenu : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
}
