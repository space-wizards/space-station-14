using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.VentCrawl.Components;

/// <summary>
/// Marks a tube as a valid entry/exit point for vent-crawling.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VentCrawlEntryComponent : Component
{
    /// <summary>
    /// Prototype used for the temporary holder entity spawned when entering the network.
    /// </summary>
    [DataField]
    public EntProtoId HolderPrototypeId = "DisposalTraversalHolder";
}
