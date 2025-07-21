using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Prototypes;

/// <summary>
/// Types of satiation, eg. Hunger, Thirst.
/// </summary>
[Prototype]
public sealed class SatiationTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The localization of the name of this type of satiation.
    /// </summary>
    [DataField]
    public readonly LocId Name;
}
