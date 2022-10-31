using Content.Shared.Xenoarchaeology.XenoArtifacts;
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
public sealed class AnalysisConsoleDestroyButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AnalysisConsoleScanUpdateState : BoundUserInterfaceState
{
    public EntityUid? Artifact;

    public bool AnalyzerConnected;

    public bool ServerConnected;

    public int? Id;

    public int? Depth;

    public int? Edges;

    public bool? Triggered;

    public string? EffectProto;

    public string? TriggerProto;

    public float? Completion;

    public AnalysisConsoleScanUpdateState(EntityUid? artifact, bool analyzerConnected, bool serverConnected,
        int? id, int? depth, int? edges, bool? triggered, string? effectProto, string? triggerProto, float? completion)
    {
        Artifact = artifact;
        AnalyzerConnected = analyzerConnected;
        ServerConnected = serverConnected;

        Id = id;
        Depth = depth;
        Edges = edges;
        Triggered = triggered;
        EffectProto = effectProto;
        TriggerProto = triggerProto;
        Completion = completion;
    }
}
