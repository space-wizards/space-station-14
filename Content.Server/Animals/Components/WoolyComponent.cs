using Content.Server.Animals.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
/// Lets an animal grow a wool solution when not hungry.
/// </summary>
[RegisterComponent, Access(typeof(WoolySystem))]
public sealed partial class WoolyComponent : Component
{
    /// <summary>
    /// What reagent to grow.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> ReagentId = "Fiber";

    /// <summary>
    /// How much wool to grow at every growth cycle.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Quantity = 25;

    /// <summary>
    /// What solution to add the wool reagent to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "wool";

    /// <summary>
    /// How long to wait before growing wool.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GrowthDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// When to next try growing wool.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
}
