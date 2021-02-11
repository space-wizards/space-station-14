using System;
using System.IO;
using Content.Shared.Eui;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Network.NetMessages
{
    public sealed class MsgEuiMessage : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgEuiMessage);

        public MsgEuiMessage(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public uint Id;
        public EuiMessageBase Message;

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
