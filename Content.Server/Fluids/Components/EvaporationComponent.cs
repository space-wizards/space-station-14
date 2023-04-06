using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Fluids.Components;

/// <summary>
/// Added to puddles that contain water so it may evaporate over time.
/// </summary>
[RegisterComponent]
public sealed class EvaporationComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick = TimeSpan.Zero;
}
