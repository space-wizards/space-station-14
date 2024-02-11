using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The client sends this to update a character profile.
    /// </summary>
    public sealed class MsgUpdateCharacter : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public int Slot;
        public ICharacterProfile Profile = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            Slot = buffer.ReadInt32();
            var length = buffer.ReadVariableInt32();
            using var stream = new MemoryStream(length);
            buffer.ReadAlignedMemory(stream, length);
            Profile = serializer.Deserialize<ICharacterProfile>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(Slot);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, Profile);
                buffer.WriteVariableInt32((int) stream.Length);
                stream.TryGetBuffer(out var segment);
                buffer.Write(segment);
            }
        }
    }
}
