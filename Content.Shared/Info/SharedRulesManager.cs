using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Info;

public abstract class SharedRulesManager
{
    [UsedImplicitly]
    public sealed class ShowRulesPopupMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public float PopupTime { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PopupTime = buffer.ReadFloat();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(PopupTime);
        }
    }
}
