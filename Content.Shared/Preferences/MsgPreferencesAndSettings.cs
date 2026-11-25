using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The server sends this before the client joins the lobby.
    /// </summary>
    public sealed class MsgPreferencesAndSettings : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public PlayerPreferences Preferences = default!;
        public GameSettings Settings = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var length = buffer.ReadVariableInt32();

            using (var stream = new MemoryStream())
            {
                buffer.ReadAlignedMemory(stream, length);
                serializer.DeserializeDirect(stream, out Preferences);
            }

            length = buffer.ReadVariableInt32();
            using (var stream = new MemoryStream())
            {
                buffer.ReadAlignedMemory(stream, length);
                serializer.DeserializeDirect(stream, out Settings);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            using (var stream = new MemoryStream())
            {
                serializer.SerializeDirect(stream, Preferences);
                buffer.WriteVariableInt32((int) stream.Length);
                stream.TryGetBuffer(out var segment);
                buffer.Write(segment);
            }

            using (var stream = new MemoryStream())
            {
                serializer.SerializeDirect(stream, Settings);
                buffer.WriteVariableInt32((int) stream.Length);
                stream.TryGetBuffer(out var segment);
                buffer.Write(segment);
            }
        }
    }
}
