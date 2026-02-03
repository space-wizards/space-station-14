using Content.Server.Ghost.Roles;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using System.Diagnostics.Metrics;

namespace Content.Server.Ghost.Components;

/// <summary>
/// If an entity with this component has a <see cref="GhostRoleComponent"/>,
/// the player who used it on hand will become the master of the ghost role.
/// 
/// </summary>
[RegisterComponent, Access(typeof(GhostRoleMasterOnUseSystem))]
public sealed partial class GhostRoleMasterOnUseComponent : Component
{
    /// <summary>
    /// After use, the master cannot be changed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Used { get; set; }

    /// <summary>
    /// Popup after clicking
    /// </summary>
    [DataField]
    public LocId UsePopup = "popup-master-add";
}
