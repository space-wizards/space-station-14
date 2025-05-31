using Content.Shared.Nutrition;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.Abilities.Borgs;

/// <summary>
/// Describes the color and flavor profile of lollipops and gumballs. Yummy!
/// </summary>
[Prototype("candyFlavor")]
public sealed partial class CandyFlavorPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The display name for this candy. Not localized.
    /// </summary>
    [DataField] public string Name { get; private set; } = "";

    /// <summary>
    /// The color of the candy.
    /// </summary>
    [DataField] public Color Color { get; private set; } = Color.White;

    /// <summary>
    /// How the candy tastes like.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<FlavorPrototype>> Flavors { get; private set; } = [];
}
