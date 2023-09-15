using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the dragon open and fully charge a certain number of rifts.
/// Depends on <see cref="NumberObjective"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(CarpRiftsConditionSystem))]
public sealed partial class CarpRiftsConditionComponent : Component
{
}
