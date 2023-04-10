using Content.Shared.Emag.Systems;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;

namespace Content.Shared.Emag.Components;

[Access(typeof(EmagSystem))]
[RegisterComponent, NetworkedComponent]
public sealed class EmagComponent : Component
{
    /// <summary>
    /// The maximum number of charges the emag can have
    /// </summary>
    [DataField("maxCharges"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxCharges = 3;

    /// <summary>
    /// The current number of charges on the emag
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite)]
    public int Charges = 3;

    /// <summary>
    /// Whether or not the emag automatically recharges over time.
    /// </summary>
    [DataField("autoRecharge"), ViewVariables(VVAccess.ReadWrite)]
    public bool AutoRecharge = true;

    /// <summary>
    /// The time it takes to regain a single charge
    /// </summary>
    [DataField("rechargeDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(90);

    /// <summary>
    /// The time when the next charge will be added
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextChargeTime = TimeSpan.MaxValue;

    /// <summary>
    /// The tag that marks an entity as immune to emags
    /// </summary>
    [DataField("emagImmuneTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string EmagImmuneTag = "EmagImmune";
}

[Serializable, NetSerializable]
public sealed class EmagComponentState : ComponentState
{
    public int MaxCharges;
    public int Charges;
    public bool AutoRecharge;
    public TimeSpan RechargeTime;
    public TimeSpan NextChargeTime;
    public string EmagImmuneTag;

    public EmagComponentState(int maxCharges, int charges, TimeSpan rechargeTime, TimeSpan nextChargeTime, string emagImmuneTag, bool autoRecharge)
    {
        MaxCharges = maxCharges;
        Charges = charges;
        RechargeTime = rechargeTime;
        NextChargeTime = nextChargeTime;
        EmagImmuneTag = emagImmuneTag;
        AutoRecharge = autoRecharge;
    }
}
