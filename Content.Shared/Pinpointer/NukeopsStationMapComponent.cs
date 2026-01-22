namespace Content.Shared.Pinpointer;

/// <summary>
/// Used to indicate that an entity with the StationMapComponent should be updated to target the TargetStation of NukeopsRuleComponent.
/// Uses the most recent active NukeopsRule when spawned, or the NukeopsRule which spawned the grid that the entity is on.
/// </summary>
[RegisterComponent]
public sealed partial class NukeopsStationMapComponent : Component
{
}
