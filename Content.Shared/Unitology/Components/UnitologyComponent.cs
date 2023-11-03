using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Unitology.Components;

/// <summary>
/// Used for marking regular unis as well as storing icon prototypes so you can see fellow unis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedUnitologySystem))]
public sealed partial class UnitologyComponent : Component
{
    /// <summary>
    /// The status icon prototype displayed for revolutionaries
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> UniStatusIcon = "UnitologyFaction";

}
