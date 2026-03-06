using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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

    /// <summary>
    ///     The cooking ingredients used in this recipe.
    /// </summary>
    [DataField(required: true)]
    public CookingIngredients Ingredients = default!;

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
}

[Serializable, DataDefinition]
public partial struct CookingIngredients(Dictionary<EntProtoId, int> solids,
    Dictionary<ProtoId<StackPrototype>, int> materials,
    Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents)
{
    [DataField]
    public Dictionary<EntProtoId, int> Solids = solids;

    [DataField]
    public Dictionary<ProtoId<StackPrototype>, int> Materials = materials;

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents = reagents;

    /// <summary>
    ///    Count the number of ingredients in a recipe for sorting the recipe list.
    ///    This makes sure that where ingredient lists overlap, the more complex
    ///    recipe is picked first.
    /// </summary>
    public readonly FixedPoint2 Count()
    {
        var solidCount = Solids.Sum(s => s.Value);
        var reagentCount = Reagents.Count;
        var materialCount = Materials.Sum(s => s.Value);

        return solidCount + reagentCount + materialCount;
    }

    /// <summary>
    ///     Get the number of times a given recipe can be made with this struct's ingredients.
    /// </summary>
    /// <param name="recipe">The recipe to attempt to make with these ingredients.</param>
    /// <returns>How many times the given recipe can be made.</returns>
    public readonly uint PortionForRecipe(CookingIngredients recipe)
    {
        var solidPortions = GetTimesFulfilled(Solids, recipe.Solids,
            (available, count) => (uint)(available / count));
        if (solidPortions == 0)
            return 0;

        var materialPortions = GetTimesFulfilled(Materials, recipe.Materials,
            (available, count) => (uint)(available / count));
        if (materialPortions == 0)
            return 0;

        var reagentPortions = GetTimesFulfilled(Reagents, recipe.Reagents,
            (available, count) => (uint)(available / count).Int());
        if (reagentPortions == 0)
            return 0;

        return new[] { solidPortions, materialPortions, reagentPortions }.Min();
    }

    private static uint GetTimesFulfilled<T, TCount>(Dictionary<T, TCount> ingredients,
        Dictionary<T, TCount> recipe,
        Func<TCount, TCount, uint> divide)
        where T : notnull
    {
        var portions = uint.MaxValue;

        foreach (var (ingredient, count) in recipe)
        {
            if (!ingredients.TryGetValue(ingredient, out var available))
                return 0;

            var ingredientPortions = divide(available, count);
            portions = Math.Min(portions, ingredientPortions);
        }

        return portions;
    }
}
