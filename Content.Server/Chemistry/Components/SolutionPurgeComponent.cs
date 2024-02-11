using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// Passively decreases a solution's quantity of reagent(s).
/// </summary>
[RegisterComponent]
[Access(typeof(SolutionPurgeSystem))]
public sealed partial class SolutionPurgeComponent : Component
{
    /// <summary>
    /// The name of the solution to detract from.
    /// </summary>
    [DataField("solution", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Solution = string.Empty;

    /// <summary>
    /// The reagent(s) to be ignored when purging the solution
    /// </summary>
    [DataField("preserve", customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> Preserve = new();

    /// <summary>
    /// Amount of reagent(s) that are purged
    /// </summary>
    [DataField("quantity", required: true), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Quantity = default!;

    /// <summary>
    /// How long it takes to purge once.
    /// </summary>
    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The time when the next purge will occur.
    /// </summary>
    [DataField("nextPurgeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPurgeTime = TimeSpan.FromSeconds(0);
}
