using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The client sends this to select a character slot.
    /// </summary>
    public sealed class MsgSelectCharacter : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public int SelectedCharacterIndex;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            SelectedCharacterIndex = buffer.ReadVariableInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(SelectedCharacterIndex);
        }
    }
}
