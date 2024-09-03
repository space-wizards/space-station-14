using Content.Server.Power.EntitySystems;
using Content.Shared.Power;

namespace Content.Server.Power.Components;

/// <inheritdoc cref="Content.Shared.Power.SharedPowerChargeComponent" />
[RegisterComponent]
[Access(typeof(PowerChargeSystem))]
public sealed partial class PowerChargeComponent : SharedPowerChargeComponent
{
    /// <summary>
    /// Change in charge per second.
    /// </summary>
    [DataField]
    public float ChargeRate { get; set; } = 0.01f;

    /// <summary>
    /// Baseline power that this machine consumes.
    /// </summary>
    [DataField("idlePower")]
    public float IdlePowerUse { get; set; }

    /// <summary>
    /// Power consumed when <see cref="SwitchedOn"/> is true.
    /// </summary>
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

    /// <summary>
    /// Whether or not the power is switched on and the entity has charged up.
    /// </summary>
    [DataField]
    public bool Active { get; set; }

    [DataField]
    public float MaxCharge { get; set; } = 1;

    /// <summary>
    /// The UI key of the UI that's used with this machine.<br/>
    /// This is used to allow machine power charging to be integrated into any ui
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Enum UiKey { get; set; } = PowerChargeUiKey.Key;

    /// <summary>
    /// Current charge value.
    /// Goes from 0 to 1.
    /// </summary>
    [DataField]
    public float Charge { get; set; } = 1;

    [ViewVariables]
    public bool NeedUIUpdate { get; set; }
}
