using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Info;

public abstract class SharedRulesManager
{
    /// <summary>
    ///     Sent by the server to show the rules to the client instantly.
    /// </summary>
    public sealed class ShowRulesPopupMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public float PopupTime { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            PopupTime = buffer.ReadFloat();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(PopupTime);
        }
    }

    /// <summary>
    ///     Sent by the server when the client needs to display the rules on join.
    /// </summary>
    public sealed class ShouldShowRulesPopupMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
        }
    }

    /// <summary>
    ///     Sent by the client when it has accepted the rules.
    /// </summary>
    public sealed class RulesAcceptedMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
        }
    }
}
