using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

/// <summary>
///    A microwave-based cooking recipe to produce an entity.
/// </summary>
[Prototype]
public sealed partial class MicrowaveMealRecipePrototype : IPrototype
{
    /// <inheritdoc />
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
    [IncludeDataField]
    public CookingIngredients Ingredients;

    /// <summary>
    ///     The resulting entity made from this recipe.
    /// </summary>
    [DataField]
    public EntProtoId Result { get; private set; } = string.Empty;

    /// <summary>
    ///     The cooking time of this recipe.
    /// </summary>
    [DataField]
    public uint Time { get; private set; } = 5;

    /// <summary>
    ///     Is this recipe unavailable in normal circumstances?
    /// </summary>
    [DataField]
    public bool SecretRecipe;

    /// <summary>
    ///     Manual priority field for recipes that take precedent over others. Higher number = picked over others.
    /// </summary>
    /// <remarks>
    ///     Secret recipes always have higher priority over non-secret recipes. Only needed when a recipe's
    ///     ingredients may overlap with another in a way that'll cause that recipe to be chosen over this one
    ///     erroneously. This field should be used as minimally as possible.
    /// </remarks>
    [DataField]
    public int Priority;
}

/// <summary>
///     A data value representing ingredients for an appliance recipe.
/// </summary>
[Serializable, DataDefinition]
public partial record struct CookingIngredients
{
    public CookingIngredients(Dictionary<EntProtoId, int> solids,
        Dictionary<ProtoId<StackPrototype>, int> stacks,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents)
    {
        Solids = solids;
        Stacks = stacks;
        Reagents = reagents;
    }

    /// <summary>
    ///     A dictionary of solid item ingredient quantities - actual items used in a recipe.
    /// </summary>
    // TODO: This should use tags or whitelists instead of entity prototype IDs
    [DataField]
    public Dictionary<EntProtoId, int> Solids { get; private set; } = new();

    /// <summary>
    ///     A dictionary of stack quantities, such as plastic sheets or cloth rolls.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<StackPrototype>, int> Stacks { get; private set; } = new();

    /// <summary>
    ///     A dictionary of reagent quantities.
    /// </summary>
    [DataField]
    // TODO: Use ReagentQuantity[]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents { get; private set; } = new();

    /// <summary>
    ///     Adds a quantity of a solid ingredient to this ingredients list.
    /// </summary>
    /// <param name="solidId">The ID of the solid to add.</param>
    /// <param name="count">How much of the solid to add. 1 by default.</param>
    public readonly void AddSolid(EntProtoId solidId, int count = 1)
    {
        if (count == 0)
            return;

        var newCount = Solids.GetValueOrDefault(solidId) + count;
        if (newCount > 0)
            Solids[solidId] = newCount;
        else
            Solids.Remove(solidId);
    }

    /// <summary>
    ///     Adds a quantity of a stack to this ingredients list.
    /// </summary>
    /// <param name="stackId">The ID of the stack to add.</param>
    /// <param name="count">How many stacks to add.</param>
    public readonly void AddStack(ProtoId<StackPrototype> stackId, int count)
    {
        if (count == 0)
            return;

        var newCount = Stacks.GetValueOrDefault(stackId) + count;
        if (newCount > 0)
            Stacks[stackId] = newCount;
        else
            Stacks.Remove(stackId);
    }

    /// <summary>
    ///     Adds a quantity of a reagent to this ingredients list.
    /// </summary>
    /// <param name="reagentId">The ID of the reagent to add.</param>
    /// <param name="quantity">The volume of the reagent to add.</param>
    public readonly void AddReagent(ProtoId<ReagentPrototype> reagentId, FixedPoint2 quantity)
    {
        if (quantity == 0)
            return;

        var newQuantity = Reagents.GetValueOrDefault(reagentId) + quantity;
        if (newQuantity > 0)
            Reagents[reagentId] = newQuantity;
        else
            Reagents.Remove(reagentId);
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
        var stackCount = Stacks.Sum(s => s.Value);

        return solidCount + reagentCount + stackCount;
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

        var stackPortions = GetTimesFulfilled(Stacks, recipe.Stacks,
            (available, count) => (uint)(available / count));
        if (stackPortions == 0)
            return 0;

        var reagentPortions = GetTimesFulfilled(Reagents, recipe.Reagents,
            (available, count) => (uint)(available / count).Int());
        if (reagentPortions == 0)
            return 0;

        return new[] { solidPortions, stackPortions, reagentPortions }.Min();
    }

    /// <summary>
    ///     Given an ingredient dictionary, and a recipe's ingredient dictionary, gets the maximum
    ///     amount of times the recipe can be fulfilled with our available ingredients.
    /// </summary>
    /// <typeparam name="T">The key of the ingredient dictionary.</typeparam>
    /// <typeparam name="TCount">A numerical quantity of the ingredient in the dictionary.</typeparam>
    /// <param name="ingredients">A dictionary of available ingredients.</param>
    /// <param name="recipe">A recipe's dictionary of required ingredients.</param>
    /// <param name="divide">Function to divide the recipe's ingredient count by our ingredient count.</param>
    /// <returns>How many times the given recipe's ingredients can be fulfilled.</returns>
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
        {
            newIngredients.AddSolid(key, count);
        }

        foreach (var (key, count) in c2.Stacks)
        {
            newIngredients.AddStack(key, count);
        }

        foreach (var (key, quantity) in c2.Reagents)
        {
            newIngredients.AddReagent(key, quantity);
        }

        return newIngredients;
    }

    public static CookingIngredients operator *(CookingIngredients c1, int scalar)
    {
        var scaledSolids = c1.Solids.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);
        var scaledStacks = c1.Stacks.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);
        var scaledReagents = c1.Reagents.ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value * scalar);

        return new(scaledSolids, scaledStacks, scaledReagents);
    }

    public static CookingIngredients operator *(CookingIngredients c1, uint scalar)
    {
        return c1 * (int)scalar;
    }
}
