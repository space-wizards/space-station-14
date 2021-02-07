using System.IO;
using Content.Shared.Preferences;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Shared.Network.NetMessages
{
    /// <summary>
    /// The client sends this to update a character profile.
    /// </summary>
    public class MsgUpdateCharacter : NetMessage
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
            var length = buffer.ReadVariableInt32();
            using var stream = buffer.ReadAlignedMemory(length);
            Profile = serializer.Deserialize<ICharacterProfile>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Slot);
            var serializer = IoCManager.Resolve<IRobustSerializer>();
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
