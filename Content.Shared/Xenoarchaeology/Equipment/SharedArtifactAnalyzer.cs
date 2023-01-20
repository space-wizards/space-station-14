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
public sealed class AnalysisConsoleDestroyButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AnalysisConsoleScanUpdateState : BoundUserInterfaceState
{
    public EntityUid? Artifact;

    public bool AnalyzerConnected;

    public bool ServerConnected;

    public bool CanScan;

    public bool CanPrint;

    public FormattedMessage? ScanReport;

    public bool Scanning;

    public TimeSpan TimeRemaining;

    public TimeSpan TotalTime;

    public AnalysisConsoleScanUpdateState(EntityUid? artifact, bool analyzerConnected, bool serverConnected, bool canScan, bool canPrint,
        FormattedMessage? scanReport, bool scanning, TimeSpan timeRemaining, TimeSpan totalTime)
    {
        Artifact = artifact;
        AnalyzerConnected = analyzerConnected;
        ServerConnected = serverConnected;
        CanScan = canScan;
        CanPrint = canPrint;

        ScanReport = scanReport;

        Scanning = scanning;
        TimeRemaining = timeRemaining;
        TotalTime = totalTime;
    }
}
