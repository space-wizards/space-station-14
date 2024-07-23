using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Spreader;

[RegisterComponent, Access(typeof(KudzuSystem)), AutoGenerateComponentPause]
public sealed partial class GrowingKudzuComponent : Component
{
    /// <summary>
    /// The next time kudzu will try to tick its growth level.
    /// </summary>
    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextTick = TimeSpan.Zero;
}
