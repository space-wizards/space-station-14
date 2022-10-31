using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class ArtifactAnalyzerComponent : Component
{
    [DataField("analysisDuration", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan AnalysisDuration = TimeSpan.FromMinutes(1.5);

    public EntityUid? LastAnalyzedArtifact;

    public ArtifactNode? LastAnalyzedNode;

    public float? LastAnalyzedCompletion;
}
