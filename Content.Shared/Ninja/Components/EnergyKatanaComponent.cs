using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Components;

// basically emag but without immune tag, TODO: make the charge thing its own component and have emag use it too
[RegisterComponent, NetworkedComponent]
public sealed class EnergyKatanaComponent : Component
{
    public EntityUid? Ninja = null;

    /// <summary>
    /// The action for dashing somewhere
    /// </summary>
    [DataField("dashAction")]
    public WorldTargetAction DashAction = new()
    {
        DisplayName = "action-name-katana-dash",
        Description = "action-desc-katana-dash",
        Priority = -14,
        Event = new KatanaDashEvent()
    };

    /// <summary>
    /// The maximum number of dash charges the katana can have
    /// </summary>
    [DataField("maxCharges"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxCharges = 3;

    /// <summary>
    /// The current number of dash charges on the katana
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite)]
    public int Charges = 3;

    /// <summary>
    /// Whether or not the katana automatically recharges over time.
    /// </summary>
    [DataField("autoRecharge"), ViewVariables(VVAccess.ReadWrite)]
    public bool AutoRecharge = true;

    /// <summary>
    /// The time it takes to regain a single dash charge
    /// </summary>
    [DataField("rechargeDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The time when the next dash charge will be added
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextChargeTime = TimeSpan.MaxValue;
}

public sealed class KatanaDashEvent : WorldTargetActionEvent { }
