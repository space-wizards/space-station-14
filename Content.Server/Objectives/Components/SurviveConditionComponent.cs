using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Just requires that the player is not dead, ignores evac and what not.
/// </summary>
[RegisterComponent, Access(typeof(SurviveConditionSystem))]
public sealed partial class SurviveConditionComponent : Component
{
}
