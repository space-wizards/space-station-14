using System.IO;
using Content.Shared.Roles;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The client sends this to update their global job priorities
    /// </summary>
    public sealed class MsgUpdateJobPriorities : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var length = buffer.ReadVariableInt32();

            using var stream = new MemoryStream();
            buffer.ReadAlignedMemory(stream, length);
            serializer.DeserializeDirect(stream, out JobPriorities);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            using var stream = new MemoryStream();
            serializer.SerializeDirect(stream, JobPriorities);
            buffer.WriteVariableInt32((int)stream.Length);
            stream.TryGetBuffer(out var segment);
            buffer.Write(segment);
        }
    }
}
