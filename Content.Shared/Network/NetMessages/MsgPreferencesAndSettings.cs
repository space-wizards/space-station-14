using System.IO;
using Content.Shared.Preferences;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Network.NetMessages
{
    /// <summary>
    /// The server sends this before the client joins the lobby.
    /// </summary>
    public class MsgPreferencesAndSettings : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgPreferencesAndSettings);

        public MsgPreferencesAndSettings(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public PlayerPreferences Preferences;
        public GameSettings Settings;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var serializer = IoCManager.Resolve<IRobustSerializer>();
            var length = buffer.ReadVariableInt32();
            using (var stream = buffer.ReadAlignedMemory(length))
            {
                serializer.DeserializeDirect(stream, out Preferences);
            }

            length = buffer.ReadVariableInt32();
            using (var stream = buffer.ReadAlignedMemory(length))
            {
                serializer.DeserializeDirect(stream, out Settings);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            var serializer = IoCManager.Resolve<IRobustSerializer>();
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
