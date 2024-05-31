using Content.Server.Objectives.Systems;
using Content.Shared.Ninja.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires the player to be a ninja and have doorjacked at least a random number of airlocks.
/// Requires <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(SharedSpaceNinjaSystem))]
public sealed partial class DoorjackConditionComponent : Component
{
    [DataField("doorsJacked"), ViewVariables(VVAccess.ReadWrite)]
    public int DoorsJacked;
}
