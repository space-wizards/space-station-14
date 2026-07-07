using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SurveillanceCamera;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SurveillanceCameraSightingRecorderComponent : Component
{
    [DataField]
    public int DetectionRange { get; set; } = 10;

    [DataField]
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(5f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
