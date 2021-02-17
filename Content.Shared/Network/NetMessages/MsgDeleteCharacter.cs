using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.NetMessages
{
    /// <summary>
    /// The client sends this to delete a character profile.
    /// </summary>
    public class MsgDeleteCharacter : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgDeleteCharacter);

        public MsgDeleteCharacter(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public int Slot;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Slot = buffer.ReadInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Slot);
        }
    }
}
