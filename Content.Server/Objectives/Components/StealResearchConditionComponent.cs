using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires the player to be a ninja and have stolen at least a random number of technologies.
/// Requires <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem))]
public sealed partial class StealResearchConditionComponent : Component
{
}
