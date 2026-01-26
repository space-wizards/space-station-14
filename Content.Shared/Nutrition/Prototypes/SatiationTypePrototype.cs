using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Prototypes;

/// <summary>
/// Types of satiation, eg. Hunger, Thirst.
/// </summary>
[Prototype]
public sealed partial class SatiationTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The localization of the name of this type of satiation.
    /// </summary>
    [DataField]
    public LocId Name { get; private set; }
}
