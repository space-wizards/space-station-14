using System.IO;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Shared.Preferences
{
    public abstract class SharedPreferencesManager
    {
        /// <summary>
        /// The server sends this before the client joins the lobby.
        /// </summary>
        protected class MsgPreferencesAndSettings : NetMessage
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
                var length = buffer.ReadInt32();
                var bytes = buffer.ReadBytes(length);
                using (var stream = new MemoryStream(bytes))
                {
                    Preferences = serializer.Deserialize<PlayerPreferences>(stream);
                }
                length = buffer.ReadInt32();
                bytes = buffer.ReadBytes(length);
                using (var stream = new MemoryStream(bytes))
                {
                    Settings = serializer.Deserialize<GameSettings>(stream);
                }
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, Preferences);
                    buffer.Write((int)stream.Length);
                    buffer.Write(stream.ToArray());
                }
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, Settings);
                    buffer.Write((int)stream.Length);
                    buffer.Write(stream.ToArray());
                }
            }
        }

        /// <summary>
        /// The client sends this to store preferences on the server.
        /// </summary>
        protected class MsgPreferences : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgPreferences);

            public MsgPreferences(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public PlayerPreferences Preferences;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                var length = buffer.ReadInt32();
                var bytes = buffer.ReadBytes(length);
                using (var stream = new MemoryStream(bytes))
                {
                    Preferences = serializer.Deserialize<PlayerPreferences>(stream);
                }
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, Preferences);
                    buffer.Write((int)stream.Length);
                    buffer.Write(stream.ToArray());
                }
            }
        }
    }
}
