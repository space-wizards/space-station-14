using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

/// <summary>
/// A recipe for space microwaves.
/// </summary>
[Prototype("microwaveMealRecipe")]
public sealed partial class FoodRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Group = "Other";

    [DataField("reagents")]
    private Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> _ingsReagents = new();

    [DataField("solids")]
    private Dictionary<EntProtoId, FixedPoint2> _ingsSolids = new();

    [DataField(required: true)] 
    public EntProtoId Result;

    [DataField("time")]
    public uint CookTime { get; private set; } = 5;

    // TODO Turn this into a ReagentQuantity[]
    public IReadOnlyDictionary<ProtoId<ReagentPrototype>, FixedPoint2> IngredientsReagents => _ingsReagents;
    public IReadOnlyDictionary<EntProtoId, FixedPoint2> IngredientsSolids => _ingsSolids;

    /// <summary>
    /// Is this recipe unavailable in normal circumstances?
    /// </summary>
    [DataField]
    public bool SecretRecipe;

    /// <summary>
    /// Count the number of ingredients in a recipe for sorting the recipe list.
    /// This makes sure that where ingredient lists overlap, the more complex
    /// recipe is picked first.
    /// </summary>
    public FixedPoint2 IngredientCount()
    {
        return _ingsReagents.Count + _ingsSolids.Values.Sum();
    }
}
