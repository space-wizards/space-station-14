using Content.Shared.Emag.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Emag.Components;

/// <summary>
/// Something with limited charges that can be recharged automatically.
/// Requires LimitedChargesComponent to function.
/// </summary>
[Access(typeof(LimitedChargesSystem))]
[RegisterComponent]
public sealed class AutoRechargeComponent : Component
{
    /// <summary>
    /// The time it takes to regain a single charge
    /// </summary>
    [DataField("rechargeDuration")]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(90);

    /// <summary>
    /// The time when the next charge will be added
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextChargeTime = TimeSpan.MaxValue;
}
