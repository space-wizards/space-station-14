using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Restricts which reagents may be refilled into a specific solution.
/// Enforced by RefillReagentWhitelistSystem during solution transfers.
/// </summary>
[RegisterComponent, NetworkedComponent, Access]
public sealed partial class RefillReagentWhitelistComponent : Component
{
    /// <summary>
    /// The name of the solution on this entity that is subject to the whitelist (e.g. "cube").
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    /// List of allowed reagent prototype IDs. Incoming transfers containing any reagent not in this list will be blocked.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> Allowed = [];

    /// <summary>
    /// Optional popup localization id to show when a transfer is blocked.
    /// If null, the default from GetSolutionTransferWhitelistEvent will be used.
    /// </summary>
    [DataField]
    public LocId? Popup;
}
