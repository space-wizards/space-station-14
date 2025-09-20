using Content.Client.Charges.EntitySystems;
using Content.Client.Charges.UI;

namespace Content.Client.Charges.Components;

/// <summary>
/// Exposes limited charges information via item status control.
/// </summary>
/// <remarks>
/// Shows the current charges out of maximum charges.
/// </remarks>
/// <seealso cref="ChargeItemStatusSystem"/>
/// <seealso cref="ChargeStatusControl"/>
[RegisterComponent]
public sealed partial class ChargeItemStatusComponent : Component
{
    /// <summary>
    /// Whether to show a recovery timer if auto-recharge is available.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowRechargeTimer = true;
}
