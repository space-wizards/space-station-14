using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

[RegisterComponent]
public sealed class ActiveArtifactAnalyzerComponent : Component
{
    [DataField("startTime", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan StartTime;

    [ViewVariables]
    public EntityUid Artifact;

    public SoundSpecifier ScanningSound = new SoundPathSpecifier("/Audio/Machines/scan_loop.ogg");
    public SoundSpecifier ScanFinishedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
    public IPlayingAudioStream? LoopStream;
}
