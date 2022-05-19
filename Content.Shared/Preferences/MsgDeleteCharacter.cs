using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The client sends this to delete a character profile.
    /// </summary>
    public sealed class MsgDeleteCharacter : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

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
