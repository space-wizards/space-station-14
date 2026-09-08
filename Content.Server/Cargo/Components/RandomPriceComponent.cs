using Content.Server.Cargo.Systems;

namespace Content.Server.Cargo.Components;

/// <summary>
/// Adds a random value between 0 and X to an entity's sell value.
/// </summary>
[RegisterComponent, Access(typeof(PricingSystem))]
public sealed partial class RandomPriceComponent : Component
{
    /// <summary>
    /// The max random price the entity may be priced at. Non-inclusive.
    /// </summary>
    [DataField(required: true)]
    public double MaxRandomPrice;

    /// <summary>
    /// How the random pricing modifier (0.0 - 1.0) should be distributed.
    /// </summary>
    [DataField]
    public RandomPricingCurve PricingCurve = RandomPricingCurve.Cubed;

    /// <summary>
    /// The generated price for the specific entity.
    /// </summary>
    [DataField]
    public double? RandomPrice = null;
}

/// <summary>
/// The random distribution used when generating a random price.
/// </summary>
public enum RandomPricingCurve
{
    Linear,
    Squared,
    Cubed,
}
