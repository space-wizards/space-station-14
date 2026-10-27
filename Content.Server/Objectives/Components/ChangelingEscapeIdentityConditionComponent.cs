using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the changeling:
/// 1. Is on the emergency shuttle when docking to CentComm
/// 2. Is currently transformed into the target identity
/// 3. Is wearing an ID card with the target's name
/// Depends on <see cref="TargetObjectiveComponent"/> to know which target identity to check.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingEscapeIdentityConditionSystem))]
public sealed partial class ChangelingEscapeIdentityConditionComponent : Component
{
    /// <summary>
    /// The character name of the target, saved during objective assignment.
    /// </summary>
    [DataField]
    public string? TargetName;
}
