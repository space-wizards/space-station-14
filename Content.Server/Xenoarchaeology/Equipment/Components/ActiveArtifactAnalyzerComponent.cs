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
    /// When did the scanning start or last resume?
    /// </summary>
    [DataField("startTime", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan StartTime;

    /// <summary>
    /// When pausing, this will store the duration the scan has already been running for.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AccumulatedRunTime;

    /// <summary>
    /// Is analysis paused?
    /// It could be when the Artifact Analyzer has no power, for example.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AnalysisPaused = false;

    /// <summary>
    /// What is being scanned?
    /// </summary>
    [DataField]
    public EntityUid Artifact;
}
