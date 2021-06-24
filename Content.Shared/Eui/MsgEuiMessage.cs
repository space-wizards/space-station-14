#nullable enable
using System;
using System.IO;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Eui
{
    public sealed class MsgEuiMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public uint Id;
        public EuiMessageBase Message = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Id = buffer.ReadUInt32();

            var ser = IoCManager.Resolve<IRobustSerializer>();
            var len = buffer.ReadVariableInt32();
            var stream = buffer.ReadAlignedMemory(len);
            Message = ser.Deserialize<EuiMessageBase>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Id);
            var stream = new MemoryStream();

            var ser = IoCManager.Resolve<IRobustSerializer>();
            ser.Serialize(stream, Message);
            var length = (int)stream.Length;
            buffer.WriteVariableInt32(length);
            buffer.Write(stream.GetBuffer().AsSpan(0, length));
        }
    }
}
