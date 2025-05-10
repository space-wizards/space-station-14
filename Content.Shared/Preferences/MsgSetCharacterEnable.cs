using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The client sends this to enable or disable the character in a character slot
    /// </summary>
    public sealed class MsgSetCharacterEnable : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public int CharacterIndex;
        public bool EnabledValue;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            CharacterIndex = buffer.ReadVariableInt32();
            EnabledValue = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.WriteVariableInt32(CharacterIndex);
            buffer.Write(EnabledValue);
        }
    }
}
