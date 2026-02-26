using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.VariationPass;

/// <summary>
/// A bodge component to spawn the given entities near the lathe of the given prototype in lieu of mapping effort
/// </summary>
[RegisterComponent]
public sealed partial class SupplyNearLatheVariationPassComponent : Component
{
    /// <summary>
    /// The prototype of the lathe to look for
    /// </summary>
    [DataField(required: true)]
    public EntProtoId LathePrototype;

    /// <summary>
    /// The entity to spawn on said lathe
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EntityToSpawn;
}
