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
                using (var stream = buffer.ReadAsStream(length))
                {
                    Preferences = serializer.Deserialize<PlayerPreferences>(stream);
                }

                length = buffer.ReadInt32();
                using (var stream = buffer.ReadAsStream(length))
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
        /// The client sends this to select a character slot.
        /// </summary>
        protected class MsgSelectCharacter : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgSelectCharacter);

            public MsgSelectCharacter(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public int SelectedCharacterIndex;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                SelectedCharacterIndex = buffer.ReadInt32();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(SelectedCharacterIndex);
            }
        }

        /// <summary>
        /// The client sends this to update a character profile.
        /// </summary>
        protected class MsgUpdateCharacter : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgUpdateCharacter);

            public MsgUpdateCharacter(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public int Slot;
            public ICharacterProfile Profile;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Slot = buffer.ReadInt32();
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                var length = buffer.ReadInt32();
                using var stream = buffer.ReadAsStream(length);
                Profile = serializer.Deserialize<ICharacterProfile>(stream);
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Slot);
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, Profile);
                    buffer.Write((int)stream.Length);
                    buffer.Write(stream.ToArray());
                }
            }
        }
    }
}
