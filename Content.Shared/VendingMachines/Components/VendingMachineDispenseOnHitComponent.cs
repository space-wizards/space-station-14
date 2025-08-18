using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.VendingMachines.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class VendingMachineDispenseOnHitComponent : Component
{
    /// <summary>
    ///     The chance that a vending machine will randomly dispense an item on hit.
    ///     Chance is 0 if null.
    /// </summary>
    [DataField]
    public float DispenseOnHitChance;

    /// <summary>
    ///     The minimum amount of damage that must be done per hit to have a chance
    ///     of dispensing an item.
    /// </summary>
    [DataField]
    public float DispenseOnHitThreshold;

    /// <summary>
    ///     Amount of time in seconds that need to pass before damage can cause a vending machine to eject again.
    ///     This value is separate to <see cref="VendingMachineComponent.EjectDelay"/> because that value might be
    ///     0 for a vending machine for legitimate reasons (no desired delay/no eject animation)
    ///     and can be circumvented with forced ejections.
    /// </summary>
    [DataField]
    public TimeSpan DispenseOnHitCooldown = TimeSpan.FromSeconds(1.0);

    /// <summary>
    ///
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? DispenseOnHitEnd;

    [ViewVariables]
    public bool DispenseOnHitCoolingDown => DispenseOnHitEnd != null;
}
