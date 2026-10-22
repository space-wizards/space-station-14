using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.FeedbackSystem;

/// <summary>
/// When clients receive this message a popup will appear with the contents from the given prototypes.
/// </summary>
public sealed class FeedbackPopupMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    /// <summary>
    /// When true, the popup prototypes specified in this message will be removed from the client's list of feedback popups.
    /// If no prototypes are specified, all popups will be removed.
    /// </summary>
    /// <remarks>If this is false and the list of prototypes is empty, the message will be ignored</remarks>
    public bool Remove { get; set; }
    public List<ProtoId<FeedbackPopupPrototype>>? FeedbackPrototypes;
    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Remove = buffer.ReadBoolean();
        buffer.ReadPadBits();

        var count = buffer.ReadVariableInt32();
        FeedbackPrototypes = [];

        for (var i = 0; i < count; i++)
        {
            FeedbackPrototypes.Add(new ProtoId<FeedbackPopupPrototype>(buffer.ReadString()));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Remove);
        buffer.WritePadBits();
        buffer.WriteVariableInt32(FeedbackPrototypes?.Count ?? 0);

        if (FeedbackPrototypes == null)
            return;

        foreach (var proto in FeedbackPrototypes)
        {
            buffer.Write(proto);
        }
    }
}

/// <summary>
/// Sent from the server to open the feedback popup.
/// </summary>
public sealed class OpenFeedbackPopupMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) { }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) { }
}
