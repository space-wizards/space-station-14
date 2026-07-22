using Content.Shared.VendingMachines.Components;

namespace Content.Server.VendingMachines.Components;

[RegisterComponent]
public sealed partial class VendingMachineDispenseOnHitComponent : Component
{
    [ViewVariables]
    public bool CoolingDown => End != null;

    [DataField]
    public TimeSpan? End;

    /// <summary>
    /// The chance that a vending machine will randomly dispense an item on hit.
    /// </summary>
    [DataField]
    public float Chance = 0.25f;

    /// <summary>
    /// The minimum amount of damage that must be done per hit to have a chance of dispensing an item.
    /// </summary>
    [DataField]
    public float Threshold = 2f;

    /// <summary>
    /// Amount of time that needs to pass before damage can cause a vending machine to eject again.
    /// This is separate to <see cref="VendingMachineEjectComponent.EjectDelay"/> because that value might be
    /// 0 for a vending machine for legitimate reasons (no desired delay/no eject animation)
    /// and can be circumvented with forced ejections.
    /// </summary>
    [DataField]
    public TimeSpan? Cooldown = TimeSpan.FromSeconds(1.0);
}
