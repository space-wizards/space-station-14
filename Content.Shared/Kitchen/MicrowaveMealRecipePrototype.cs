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

/// <summary>
///     A data value representing ingredients for an appliance recipe.
/// </summary>
[Serializable, DataDefinition]
public partial record struct CookingIngredients
{
    public CookingIngredients(Dictionary<EntProtoId, int> solids,
        Dictionary<ProtoId<StackPrototype>, int> materials,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents)
    {
        Solids = solids;
        Materials = materials;
        Reagents = reagents;
    }

    /// <summary>
    ///     A dictionary of solid item ingredient quantities - actual items used in a recipe.
    /// </summary>
    // TODO: This should use tags or whitelists instead of entity prototype IDs
    [DataField]
    public Dictionary<EntProtoId, int> Solids { get; private set; } = new();

    /// <summary>
    ///     A dictionary of stack material quantities, such as plastic sheets or cloth rolls.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<StackPrototype>, int> Materials { get; private set; } = new();

    /// <summary>
    ///     A dictionary of reagent quantities.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents { get; private set; } = new();

    public readonly void AddSolid(EntProtoId solidId, int count = 1)
    {
        Solids[solidId] = Solids.GetValueOrDefault(solidId) + count;
    }

    public readonly void AddMaterial(ProtoId<StackPrototype> materialId, int count)
    {
        Materials[materialId] = Materials.GetValueOrDefault(materialId) + count;
    }

    public readonly void AddReagent(ProtoId<ReagentPrototype> reagentId, FixedPoint2 quantity)
    {
        Reagents[reagentId] = Reagents.GetValueOrDefault(reagentId) + quantity;
    }

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

    public static CookingIngredients operator +(CookingIngredients c1, CookingIngredients c2)
    {
        var newIngredients = c1;

        foreach (var (key, count) in c2.Solids)
            newIngredients.AddSolid(key, count);

        foreach (var (key, count) in c2.Materials)
            newIngredients.AddMaterial(key, count);

        foreach (var (key, quantity) in c2.Reagents)
            newIngredients.AddReagent(key, quantity);

        return newIngredients;
    }

    public static CookingIngredients operator *(CookingIngredients c1, int scalar)
    {
        var scaledSolids = c1.Solids.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);
        var scaledMaterials = c1.Materials.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);
        var scaledReagents = c1.Reagents.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);

        return new(scaledSolids, scaledMaterials, scaledReagents);
    }

    public static CookingIngredients operator *(CookingIngredients c1, uint scalar)
    {
        return c1 * (int)scalar;
    }
}
