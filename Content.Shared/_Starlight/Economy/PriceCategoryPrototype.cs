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

    [DataField]
    public int Min = 0;

    [DataField]
    public int Max;
}
