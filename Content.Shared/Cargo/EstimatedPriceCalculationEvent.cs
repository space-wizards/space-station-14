using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo;

/// <summary>
/// Raised broadcast for an entity prototype to determine its estimated price.
/// </summary>
[ByRefEvent]
public record struct EstimatedPriceCalculationEvent()
{
    public EntityPrototype Prototype;

    /// <summary>
    /// The total price of the entity.
    /// </summary>
    public double Price = 0;

    /// <summary>
    /// Whether this event was already handled.
    /// </summary>
    public bool Handled = false;
}