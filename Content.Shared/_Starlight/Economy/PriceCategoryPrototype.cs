using Robust.Shared.Prototypes;

namespace Content.Shared.Economy;

/// <summary>
/// Prototype that defines a price category range for price generation
/// </summary>
[Prototype("priceCategory")]
public sealed partial class PriceCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("min", required: true)]
    public int Min;

    [DataField("max", required: true)]
    public int Max;
}
