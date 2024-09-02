using Content.Server.Animals.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Animals.Components;

/// <summary>
///     Lets an entity produce wool fibers. Uses hunger if present.
/// </summary>

[RegisterComponent, Access(typeof(WoolySystem))]
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
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 Quantity = 25;

    /// <summary>
    ///     The amount of nutrient consumed on update.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HungerUsage = 10f;

    /// <summary>
    ///     How long to wait before growing wool.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GrowthDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     When to next try growing wool.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
}
