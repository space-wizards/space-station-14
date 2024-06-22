using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Animals;

/// <summary>
///     Gives the ability to produce a solution;
///     produces endlessly if the owner does not have a HungerComponent.
/// </summary>
[RegisterComponent]
[Virtual]
[Access(typeof(SharedUdderComponent)), AutoGenerateComponentPause]
public partial class SharedUdderComponent : Component
{
    /// <summary>
    ///     The reagent to produce.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> ReagentId = default!;

    /// <summary>
    ///     The name of <see cref="Solution"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string SolutionName = "udder";

    /// <summary>
    ///     The solution to add reagent to.
    /// </summary>
    [DataField]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    ///     The amount of reagent to be generated on update.
    /// </summary>
    [DataField]
    public FixedPoint2 QuantityPerUpdate = 25;

    /// <summary>
    ///     The amount of nutrient consumed on update.
    /// </summary>
    [DataField]
    public float HungerUsage = 10f;

    /// <summary>
    ///     How long to wait before producing.
    /// </summary>
    [DataField]
    public TimeSpan GrowthDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     When to next try to produce.
    /// </summary>
    //[DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), Access(typeof(SharedUdderSystem))]
    //public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
    [AutoPausedField, Access(typeof(SharedUdderSystem))]
    public TimeSpan NextGrowth = TimeSpan.Zero;
}
