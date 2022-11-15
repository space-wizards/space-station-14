using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.Sponsors;

/// <summary>
/// Server sends sponsoring info to client on connect only if user is sponsor
/// </summary>
public sealed class MsgSponsoringInfo : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public bool IsSponsor;
    public bool AllowedNeko;
    
    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        IsSponsor = buffer.ReadBoolean();
        AllowedNeko = buffer.ReadBoolean();
        buffer.ReadPadBits();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(IsSponsor);
        buffer.Write(AllowedNeko);
        buffer.WritePadBits();
    }
}
