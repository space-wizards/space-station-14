using Content.Server.Charges.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Charges.Components;

/// <summary>
/// Something with limited charges that can be recharged automatically.
/// Requires LimitedChargesComponent to function.
/// </summary>
// TODO: no reason this cant be predicted and server system deleted
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ChargesSystem))]
public sealed partial class AutoRechargeComponent : Component
{
    /// <summary>
    /// The time it takes to regain a single charge
    /// </summary>
    [DataField("rechargeDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(90);

    /// <summary>
    /// The time when the next charge will be added
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextChargeTime;
}
