using Content.Client.Chemistry.EntitySystems;
using Content.Client.Chemistry.UI;

namespace Content.Client.Chemistry.Components;

/// <summary>
/// Exposes a solution container's contents via a basic item status control.
/// </summary>
/// <remarks>
/// Shows the solution volume, max volume, and transfer amount.
/// </remarks>
/// <seealso cref="SolutionItemStatusSystem"/>
/// <seealso cref="SolutionStatusControl"/>
[RegisterComponent]
public sealed partial class SolutionItemStatusComponent : Component
{
    /// <summary>
    /// The ID of the solution that will be shown on the item status control.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";
}
