namespace Content.Shared.Power;

/// <summary>
/// Raised when a battery's charge or capacity changes (capacity affects relative charge percentage).
/// </summary>
[ByRefEvent]
public readonly record struct ChargeChangedEvent(float Charge, float MaxCharge);

/// <summary>
/// Raised when it is necessary to get information about battery charges.
/// </summary>
[ByRefEvent]
public sealed class GetChargeEvent : EntityEventArgs
{
    public float CurrentCharge;
    public float MaxCharge;
}

/// <summary>
/// Raised when it is necessary to change the current battery charge to a some value.
/// </summary>
[ByRefEvent]
public sealed class ChangeChargeEvent : EntityEventArgs
{
    public float OriginalValue;
    public float ResidualValue;

    public ChangeChargeEvent(float value)
    {
        OriginalValue = value;
        ResidualValue = value;
    }
}
