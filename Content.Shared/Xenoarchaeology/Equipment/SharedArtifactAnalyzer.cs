using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Xenoarchaeology.Equipment;

[Serializable, NetSerializable]
public sealed class AnalysisConsoleExtractButtonPressedMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AnalysisConsoleUpdateState(
    NetEntity? artifact,
    bool analyzerConnected,
    bool serverConnected,
    bool canScan,
    bool canPrint,
    FormattedMessage? scanReport,
    bool scanning,
    bool paused,
    TimeSpan? startTime,
    TimeSpan? accumulatedRunTime,
    TimeSpan? totalTime,
    int pointAmount,
    bool isTraversalDown
)
    : BoundUserInterfaceState
{
    public NetEntity? Artifact = artifact;
    public bool AnalyzerConnected = analyzerConnected;
    public bool ServerConnected = serverConnected;
    public bool CanScan = canScan;
    public bool CanPrint = canPrint;
    public FormattedMessage? ScanReport = scanReport;
    public bool Scanning = scanning;
    public bool Paused = paused;
    public TimeSpan? StartTime = startTime;
    public TimeSpan? AccumulatedRunTime = accumulatedRunTime;
    public TimeSpan? TotalTime = totalTime;
    public int PointAmount = pointAmount;
    public bool IsTraversalDown = isTraversalDown;
}
