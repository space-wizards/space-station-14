using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is not a member of command.
/// </summary>
[RegisterComponent, Access(typeof(NotCommandRequirementSystem))]
public sealed partial class NotCommandRequirementComponent : Component
{
}
