using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Kudzu;

[RegisterComponent]
public sealed class GrowingKudzuComponent : Component
{
    [DataField("growthLevel")]
    public int GrowthLevel = 1;

    [DataField("growthTickSkipChance")]
    public float GrowthTickSkipChange = 0.0f;

    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick = TimeSpan.Zero;
}
