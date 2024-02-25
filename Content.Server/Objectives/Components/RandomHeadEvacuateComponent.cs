using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Completion requires the target to fly to Central Command alive and free
/// </summary>
[RegisterComponent, Access(typeof(EvacuateHeadConditionSystem))]
public sealed partial class RandomHeadEvacuateComponent : Component
{
}
