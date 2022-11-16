using Robust.Shared.Serialization;

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

    public int? Id;

    public int? Depth;

    public int? Edges;

    public bool? Triggered;

    public string? EffectProto;

    public string? TriggerProto;

    public int? PointValue;

    public bool Scanning;

    public TimeSpan TimeRemaining;

    public TimeSpan TotalTime;

    public AnalysisConsoleScanUpdateState(EntityUid? artifact, bool analyzerConnected, bool serverConnected, bool canScan, bool canPrint,
        int? id, int? depth, int? edges, bool? triggered, string? effectProto, string? triggerProto, int? pointValue,
        bool scanning, TimeSpan timeRemaining, TimeSpan totalTime)
    {
        Artifact = artifact;
        AnalyzerConnected = analyzerConnected;
        ServerConnected = serverConnected;
        CanScan = canScan;
        CanPrint = canPrint;

        Id = id;
        Depth = depth;
        Edges = edges;
        Triggered = triggered;
        EffectProto = effectProto;
        TriggerProto = triggerProto;
        PointValue = pointValue;

        Scanning = scanning;
        TimeRemaining = timeRemaining;
        TotalTime = totalTime;
    }
}
