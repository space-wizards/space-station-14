using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.EquipmentForceFacing;

/// <summary>
/// Only used for griffy suit's family guy death pose. Attempts to force the equippee to face north when their mobstate changes to critical or incapacitated
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EquipmentForceFacingComponent : Component
{

}
