using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Info;

/// <summary>
///  Sent by the server when the client connects to sync the client rules and displaying a popup with them if necessitated.
/// </summary>
public sealed class SendRulesInformationMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public float PopupTime { get; set; }
    public string CoreRules { get; set; } = string.Empty;
    public bool ShouldShowRules { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        PopupTime = buffer.ReadFloat();
        CoreRules = buffer.ReadString();
        ShouldShowRules = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(PopupTime);
        buffer.Write(CoreRules);
        buffer.Write(ShouldShowRules);
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
