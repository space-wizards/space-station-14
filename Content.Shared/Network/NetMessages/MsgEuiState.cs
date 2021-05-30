#nullable enable
using System;
using System.IO;
using Content.Shared.Eui;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Network.NetMessages
{
    public sealed class MsgEuiState : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgEuiState);

        public MsgEuiState(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public uint Id;
        public EuiStateBase State = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Id = buffer.ReadUInt32();

            var ser = IoCManager.Resolve<IRobustSerializer>();
            var len = buffer.ReadVariableInt32();
            var stream = buffer.ReadAlignedMemory(len);
            State = ser.Deserialize<EuiStateBase>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Id);
            var stream = new MemoryStream();

            var ser = IoCManager.Resolve<IRobustSerializer>();
            ser.Serialize(stream, State);
            var length = (int)stream.Length;
            buffer.WriteVariableInt32(length);
            buffer.Write(stream.GetBuffer().AsSpan(0, length));
        }
    }
}
