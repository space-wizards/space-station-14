using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Stacks;


namespace Content.Server.Animals.Components;

/// <summary>
///     Lets an entity be sheared by a tool to consume a reagent to spawn an amount of an item.
///     For example, sheep can be sheared to consume woolSolution to spawn cotton.
/// </summary>

[RegisterComponent]
public sealed partial class ShearableComponent : Component
{
    /// <summary>
    ///     A pre-existing solution inside the target entity that will be subtracted from upon being sheared.
    ///     e.g. wooly creatures use "wool"
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string TargetSolutionName = string.Empty;

    /// <summary>
    ///     An ID of a stackable product that will be spawned upon the creature being sheared.
    ///     e.g. Sheep use "Cotton".
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<StackPrototype> ShearedProductID;

    /// <summary>
    ///     How many products will be spawned per solution.
    ///     A value of 5 will spawn 5 products for every 1u of solution. 25u of solution would spawn 125 product.
    ///     A value of 0.2 will spawn 0.2 products for every 1u of solution. 25u of solution would spawn 5 product.
    ///     Keep in mind, only up to the maximum stack of the specified product will be spawned,
    ///     the remaining solution will be truncated and left unchanged. In these cases the player can shear more than once to get more.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float ProductsPerSolution = 0;

    /// <summary>
    ///     The solution to add reagent to.
    /// </summary>
    [DataField]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    ///     The "Quality" of the target item that allows this entity to be sheared.
    ///     For example, Wirecutters have the "cutting" quality.
    ///     Leave undefined for no tool required.
    /// </summary>
    [DataField]
    public string ToolQuality = string.Empty;

}
