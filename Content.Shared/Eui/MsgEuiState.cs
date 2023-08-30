using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Eui
{
    public sealed class MsgEuiState : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public uint Id;
        public EuiStateBase State = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer ser)
        {
            Id = buffer.ReadUInt32();

            var len = buffer.ReadVariableInt32();
            var stream = buffer.ReadAlignedMemory(len);
            State = ser.Deserialize<EuiStateBase>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer ser)
        {
            buffer.Write(Id);
            var stream = new MemoryStream();

            ser.Serialize(stream, State);
            var length = (int)stream.Length;
            buffer.WriteVariableInt32(length);
            buffer.Write(stream.GetBuffer().AsSpan(0, length));
        }
    }
}
