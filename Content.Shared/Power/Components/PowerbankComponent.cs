namespace Content.Shared.Power.Components;

/// <summary>
/// Entities that can charge batteries and battery-operating entities via a doAfter.
/// </summary>
[RegisterComponent]
public sealed partial class PowerbankComponent : Component
{
    /// <summary>
    /// How much power the powerbank transfers each doAfter.
    /// Should be a clean multiple of the max capacity.
    /// </summary>
    [DataField(required: true)]
    public float TransferAmount = 250f;

    /// <summary>
    /// The duration of the doAfter for each recharge.
    /// </summary>
    [DataField]
    public TimeSpan ChargeDelay = TimeSpan.FromSeconds(1);
}
