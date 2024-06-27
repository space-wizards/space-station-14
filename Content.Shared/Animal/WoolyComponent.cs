using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Animals;

/// <summary>
///     Gives the ability to produce wool fibers;
///     produces endlessly if the owner does not have a HungerComponent.
/// </summary>
[RegisterComponent, Access(typeof(WoolySystem)), AutoGenerateComponentPause, NetworkedComponent]
public sealed partial class WoolyComponent : Component
{
    /// <summary>
    ///     The reagent to grow.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<ReagentPrototype> ReagentId = "Fiber";

    /// <summary>
    ///     The name of <see cref="Solution"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string SolutionName = "wool";

    /// <summary>
    ///     The solution to add reagent to.
    /// </summary>
    [DataField]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    ///     The amount of reagent to be generated on update.
    /// </summary>
    [DataField]
    public FixedPoint2 Quantity = 25;

    /// <summary>
    ///     The amount of nutrient consumed on update.
    /// </summary>
    [DataField]
    public float HungerUsage = 10f;

    /// <summary>
    ///     How long to wait before growing wool.
    /// </summary>
    [DataField]
    public TimeSpan GrowthDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     When to next try growing wool.
    /// </summary>
    [DataField, AutoPausedField, Access(typeof(WoolySystem))]
    public TimeSpan NextGrowth = TimeSpan.Zero;
}
