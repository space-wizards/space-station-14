#nullable enable
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.NetMessages
{
    /// <summary>
    /// The client sends this to select a character slot.
    /// </summary>
    public class MsgSelectCharacter : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgSelectCharacter);

        public MsgSelectCharacter(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

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
