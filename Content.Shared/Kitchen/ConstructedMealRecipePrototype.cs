using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

/// <summary>
/// Defines a type of meal recipe made in construction graphs.
/// See <see cref="ConstructedMealRecipePrototype"/>.
/// </summary>
[Prototype("constructedMeal")]
public sealed partial class ConstructedMealPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}

/// <summary>
/// Defines a specific meal recipe made in construction graphs.
/// Gives a unique name and sprite to a special recipe.
/// </summary>
[Prototype("constructedMealRecipe")]
public sealed partial class ConstructedMealRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Meal type this recipe is for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ConstructedMealPrototype> Meal = string.Empty;

    /// <summary>
    /// Tags that must be present in the meal's added items.
    /// Each item added to the meal can only provide 1 tag.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<TagPrototype>, uint> Tags = new();

    /// <summary>
    /// Item prototype that this recipe would seem to create.
    /// Name and description are copied from this, but nothing else.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype = string.Empty;

    /// <summary>
    /// Priority for picking this recipe over others if multiple are satisfied.
    /// More unique recipes should be higher priority than others.
    /// Recipes with more generic contents should have negative priority.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Solution to add to the meal's food solution as a reward for completing the recipe.
    /// </summary>
    [DataField]
    public Solution? BonusSolution;

    // TODO: bonus components for arnolds pizza
}
