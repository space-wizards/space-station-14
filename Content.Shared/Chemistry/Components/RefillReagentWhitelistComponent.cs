using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Restricts which reagents may be refilled into a specific solution.
/// Enforced by RefillReagentWhitelistSystem during solution transfers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RefillReagentWhitelistComponent : Component
{
    /// <summary>
    /// The name of the solution on this entity that is subject to the whitelist (e.g. "cube").
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";

    /// <summary>
    /// List of allowed reagent prototype IDs. Incoming transfers containing any reagent not in this list will be blocked.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<ReagentPrototype>> Allowed = new();

    /// <summary>
    /// Optional popup localization id to show when a transfer is blocked.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? Popup;
}
