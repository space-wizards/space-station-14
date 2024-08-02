using Content.Server.Animals.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Animals.Components;

/// <summary>
///     Lets an entity be sheared by a tool to consume a reagent to spawn an amount of an item.
///     For example, sheep can be sheared to consume woolSolution to spawn cotton.
/// </summary>

[RegisterComponent]
public sealed partial class ShearableComponent : Component
{
    /// <summary>
    ///     A pre-existing solution inside the target entity that will be removed upon being sheared.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string TargetSolutionName = string.Empty;

    /// <summary>
    ///     A product that will be spawned upon the creature being sheared.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string ShearedProductID = string.Empty;

    /// <summary>
    ///     How many products will be spawned per solution.
    ///     For example, 0.2 would spawn 1 shearedProductID for every 5 targetSolutionID consumed.
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
