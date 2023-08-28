using Content.Shared.Anomaly;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Anomaly.Components;

[RegisterComponent, Access(typeof(SharedAnomalySystem))]
public sealed partial class GeneratingAnomalyGeneratorComponent : Component
{
    /// <summary>
    /// When the generating period will end.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EndTime = TimeSpan.Zero;

    public IPlayingAudioStream? AudioStream;
}
