using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

[RegisterComponent]
public sealed class ActiveArtifactAnalyzerComponent : Component
{
    [DataField("startTime", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan StartTime;

    [ViewVariables]
    public EntityUid Artifact;
}
