using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// Passively decreases a solution's quantity of reagent(s).
/// </summary>
[RegisterComponent]
[Access(typeof(SolutionPurgeSystem))]
public sealed class SolutionPurgeComponent : Component
{
    /// <summary>
    /// The name of the solution to detract from.
    /// </summary>
    [DataField("solution", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Solution = string.Empty;

    /// <summary>
    /// The reagent(s) to be ignored when purging the solution
    /// </summary>
    [DataField("reserved"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> Reserve = default!;

    /// <summary>
    /// Amount of reagent(s) that are purged
    /// </summary>
    [DataField("purgeQuantity", required: true), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Purge = default!;

    /// <summary>
    /// How long it takes to purge once.
    /// </summary>
    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The time when the next purge will occur.
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPurgeTime = TimeSpan.FromSeconds(0);
}
