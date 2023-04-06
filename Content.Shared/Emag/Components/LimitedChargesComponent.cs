using Content.Shared.Emag.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Emag.Components;

/// <summary>
/// Something with limited charges that can be recharged automatically.
/// </summary>
[Access(typeof(LimitedChargesSystem))]
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class LimitedChargesComponent : Component
{
    /// <summary>
    /// The maximum number of charges
    /// </summary>
    [DataField("maxCharges"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public int MaxCharges = 3;

    /// <summary>
    /// The current number of charges
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public int Charges = 3;

    /// <summary>
    /// Whether or not it automatically recharges over time.
    /// </summary>
    [DataField("autoRecharge"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool AutoRecharge = true;

    /// <summary>
    /// The time it takes to regain a single charge
    /// </summary>
    [DataField("rechargeDuration"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(90);

    /// <summary>
    /// The time when the next charge will be added
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan NextChargeTime = TimeSpan.MaxValue;
}
