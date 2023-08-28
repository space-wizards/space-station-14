using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Spreader;

[RegisterComponent, Access(typeof(KudzuSystem))]
public sealed partial class GrowingKudzuComponent : Component
{
    /// <summary>
    /// At level 3 spreading can occur; prior to that we have a chance of increasing our growth level and changing our sprite.
    /// </summary>
    [DataField("growthLevel")]
    public int GrowthLevel = 1;

    /// <summary>
    /// The next time kudzu will try to tick its growth level.
    /// </summary>
    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick = TimeSpan.Zero;
}
