using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets this objective's target to the exterminator's target override, if it has one.
/// If not it will be random.
/// </summary>
[RegisterComponent, Access(typeof(TerminatorTargetOverrideSystem))]
public sealed partial class TerminatorTargetOverrideComponent : Component
{
}
