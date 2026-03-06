using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

/// <summary>
///    A recipe for space microwaves.
/// </summary>
[Prototype("microwaveMealRecipe")]
public sealed partial class FoodRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of the recipe.
    /// </summary>
    /// <remarks>
    ///     This is used to sort recipes in alphabetical order in the guidebook.
    /// </remarks>
    [DataField("name")]
    private LocId _name = string.Empty;

    public string Name => Loc.GetString(_name);

    /// <summary>
    ///     The guidebook grouping for this recipe.
    /// </summary>
    [DataField]
    public string Group = "Other";

    // TODO: Use AvailableIngredients struct

    /// <summary>
    ///     The reagent ingredients used in this recipe.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents = new();

    /// <summary>
    ///     The solid ingredients used in this recipe.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntProtoId, int> Solids = new();

    /// <summary>
    ///     The material stack ingredients used in this recipe.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<StackPrototype>, int> Materials = new();

    /// <summary>
    ///     The resulting entity made from this recipe.
    /// </summary>
    [DataField]
    public EntProtoId Result { get; private set; } = string.Empty;

    /// <summary>
    ///     The cooking time of this recipe.
    /// </summary>
    [DataField("time")]
    public uint CookTime { get; private set; } = 5;

    /// <summary>
    ///     Is this recipe unavailable in normal circumstances?
    /// </summary>
    [DataField]
    public bool SecretRecipe = false;

    /// <summary>
    ///    Count the number of ingredients in a recipe for sorting the recipe list.
    ///    This makes sure that where ingredient lists overlap, the more complex
    ///    recipe is picked first.
    /// </summary>
    public FixedPoint2 IngredientCount()
    {
        var solidCount = Solids.Sum(s => s.Value);
        var reagentCount = Reagents.Count;
        var materialCount = Materials.Sum(s => s.Value);

        return solidCount + reagentCount;
    }
}
