namespace Content.Shared.Cargo;

/// <summary>
/// A directed by-ref event fired on an entity when something needs to know it's price. This value is not cached.
/// </summary>
[ByRefEvent]
public record struct PriceCalculationEvent()
{
    /// <summary>
    /// The total price of the entity.
    /// </summary>
    public double Price = 0;

    /// <summary>
    /// Whether this event was already handled.
    /// </summary>
    public bool Handled = false;
}