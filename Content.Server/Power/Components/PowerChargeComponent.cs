using Content.Server.Power.EntitySystems;
using Content.Shared.Power;

namespace Content.Server.Power.Components;

[RegisterComponent]
[Access(typeof(PowerChargeSystem))]
public sealed partial class PowerChargeComponent : SharedPowerChargeComponent
{
    // TODO: Change comments
    // 1% charge per second.
    [DataField]
    public float ChargeRate { get; set; } = 0.01f;

    // The gravity generator has two power values.
    // Idle power is assumed to be the power needed to run the control systems and interface.
    [DataField("idlePower")]
    public float IdlePowerUse { get; set; }

    // Active power is the power needed to keep the gravity field stable.
    [DataField("activePower")]
    public float ActivePowerUse { get; set; }

    /// <summary>
    /// Is the gravity generator intact?
    /// </summary>
    [DataField]
    public bool Intact { get; set; } = true;

    /// <summary>
    /// Is the power switch on?
    /// </summary>
    [DataField]
    public bool SwitchedOn { get; set; } = true;

    [ViewVariables]
    public bool Active { get; set; } = false;

    [DataField]
    public float MaxCharge { get; set; } = 1;

    /// <summary>
    /// The UI key of the UI that's used with this machine.<br/>
    /// This is used to allow machine power charging to be integrated into any ui
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Enum UiKey { get; set; } = PowerChargeUiKey.Key;

    // 0 -> 1
    [DataField]
    public float Charge { get; set; } = 1;

    [ViewVariables]
    public bool NeedUIUpdate { get; set; }
}
