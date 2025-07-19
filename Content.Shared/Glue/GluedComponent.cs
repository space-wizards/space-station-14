using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Glue;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(GlueSystem))]
public sealed partial class GluedComponent : Component
{

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan Until;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Duration;
}
