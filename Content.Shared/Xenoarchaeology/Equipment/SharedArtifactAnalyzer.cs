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
public sealed class AnalysisConsoleScanUpdateState : BoundUserInterfaceState
{
    public EntityUid? Artifact;

    public bool AnalyzerConnected;

    public AnalysisConsoleScanUpdateState(EntityUid? artifact, bool analyzerConnected)
    {
        Artifact = artifact;
        AnalyzerConnected = analyzerConnected;
    }
}
