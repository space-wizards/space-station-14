using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo;

/// <summary>
/// A directed by-ref event fired on an entity when something needs to know its price. This value is not cached.
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

/// <summary>
/// Raised broadcast for an entity prototype to determine its estimated price.
/// </summary>
/// <param name="Prototype">The prototype to estimate the price for.</param>
[ByRefEvent]
public record struct EstimatedPriceCalculationEvent(EntityPrototype Prototype)
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
