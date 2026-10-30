using Content.Shared.VendingMachines.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.VendingMachines.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class VendingMachineDispenseOnHitComponent : Component
{
    [ViewVariables]
    public bool CoolingDown => NextDispenseTime != null;

    /// <summary>
    /// The time at which the dispense-on-hit cooldown ends.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextDispenseTime;

    /// <summary>
    /// Amount of time that needs to pass before damage can cause a vending machine to eject again.
    /// This is separate to <see cref="VendingMachineEjectComponent.EjectDelay"/> because that value might be
    /// 0 for a vending machine for legitimate reasons (no desired delay/no eject animation)
    /// and can be circumvented with forced ejections.
    /// </summary>
    [DataField]
    public TimeSpan? NextDispenseDelay = TimeSpan.FromSeconds(1.0);

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
}
