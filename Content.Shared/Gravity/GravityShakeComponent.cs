using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Gravity;

/// <summary>
/// Indicates this entity is shaking due to gravity changes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class GravityShakeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("shakeTimes")]
    public int ShakeTimes;

    [DataField("nextShake", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextShake;
}
