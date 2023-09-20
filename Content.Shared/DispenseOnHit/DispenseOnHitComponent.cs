using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DispenseOnHit;

[RegisterComponent, NetworkedComponent]
public sealed partial class DispenseOnHitComponent : Component
{
    /// <summary>
    ///     The chance that a vending machine will randomly dispense an item on hit.
    ///     Chance is 0 if null.
    /// </summary>
    [DataField("chance")]
    public float? Chance;

    /// <summary>
    ///     The minimum amount of damage that must be done per hit to have a chance
    ///     of dispensing an item.
    /// </summary>
    [DataField("threshold")]
    public float? Threshold;

    /// <summary>
    ///     Amount of time in TimeSpan that need to pass before damage can cause a vending machine to eject again.
    ///     This value is separate to <see cref="VendingMachineComponent.EjectDelay"/> because that value might be
    ///     0 for a vending machine for legitimate reasons (no desired delay/no eject animation)
    ///     and can be circumvented with forced ejections.
    /// </summary>
    [DataField("delay", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.0f);

    /// <summary>
    ///    Data for understanding when the hit action was performed
    /// </summary>
    [DataField("cooldown", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan Cooldown = TimeSpan.Zero;

    public bool CoolingDown;
}
