using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.BugReport;

/// <summary>
/// Message with bug report data, which should be handled by server and used to create issue on issue tracker
/// (or some other notification).
/// </summary>
public sealed class BugReportMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public PlayerBugReportInformation ReportInformation = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        ReportInformation.BugReportTitle = buffer.ReadString();
        ReportInformation.BugReportDescription = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(ReportInformation.BugReportTitle);
        buffer.Write(ReportInformation.BugReportDescription);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
}

/// <summary>
///     Stores user specified information from a bug report.
/// </summary>
/// <remarks>
///      Clients can put whatever they want here so be careful!
/// </remarks>
public sealed class PlayerBugReportInformation
{
    public string BugReportTitle = string.Empty;
    public string BugReportDescription = string.Empty;
}
