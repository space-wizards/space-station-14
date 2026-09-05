using Content.Client.Chemistry.EntitySystems;
using Content.Client.Chemistry.UI;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Client.Chemistry.Components;

/// <summary>
/// Exposes a solution container's contents via a basic item status control.
/// </summary>
/// <remarks>
/// Shows the solution volume when <see cref="ExaminableSolutionComponent"/> is present,
/// and transfer amount when <see cref="SolutionTransferComponent"/> is present.
/// </remarks>
/// <seealso cref="SolutionItemStatusSystem"/>
/// <seealso cref="SolutionStatusControl"/>
[RegisterComponent]
public sealed partial class SolutionItemStatusComponent : Component
{
    /// <summary>
    /// The ID of the solution that will be shown on the item status control.
    /// </summary>
    [DataField]
    public string Solution = "default";

    [DataField]
    public LocId LocControlVolume = "solution-status-volume";

    [DataField]
    public LocId LocControlTransfer =  "solution-status-transfer";
}
