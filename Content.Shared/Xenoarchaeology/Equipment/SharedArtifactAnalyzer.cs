using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Xenoarchaeology.Equipment;

[Serializable, NetSerializable]
public enum ArtifactAnalzyerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class AnalysisConsoleServerSelectionMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AnalysisConsoleScanButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AnalysisConsolePrintButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AnalysisConsoleExtractButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AnalysisConsoleScanUpdateState : BoundUserInterfaceState
{
    public NetEntity? Artifact;

    public bool AnalyzerConnected;

    public bool ServerConnected;

    public bool CanScan;

    public bool CanPrint;

    public FormattedMessage? ScanReport;

    public bool Scanning;

    public TimeSpan? StartTime;

    public TimeSpan? TotalTime;

    public int PointAmount;

    public AnalysisConsoleScanUpdateState(NetEntity? artifact, bool analyzerConnected, bool serverConnected, bool canScan, bool canPrint,
        FormattedMessage? scanReport, bool scanning, TimeSpan? startTime, TimeSpan? totalTime, int pointAmount)
    {
        Artifact = artifact;
        AnalyzerConnected = analyzerConnected;
        ServerConnected = serverConnected;
        CanScan = canScan;
        CanPrint = canPrint;

        ScanReport = scanReport;

        Scanning = scanning;
        StartTime = startTime;
        TotalTime = totalTime;

        PointAmount = pointAmount;
    }
}
