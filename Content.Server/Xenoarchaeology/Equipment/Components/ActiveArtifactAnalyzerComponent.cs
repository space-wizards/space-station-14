using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// Activecomp used for tracking artifact analyzers that are currently
/// in the process of scanning an artifact.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveArtifactAnalyzerComponent : Component
{
    /// <summary>
    /// When did the scanning start?
    /// </summary>
    [DataField("startTime", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan StartTime;

    /// <summary>
    /// Is analysis paused?
    /// It could be when the Artifact Analyzer has no power, for example.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AnalysisPaused = false;

    /// <summary>
    /// Accumulated seconds the analysis has been going on for.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ScanProgressSec = 0;

    /// <summary>
    /// What is being scanned?
    /// </summary>
    [DataField]
    public EntityUid Artifact;
}
