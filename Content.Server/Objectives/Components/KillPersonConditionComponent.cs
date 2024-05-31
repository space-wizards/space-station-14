using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target dies or, if <see cref="RequireDead"/> is false, is not on the emergency shuttle.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(KillPersonConditionSystem))]
public sealed partial class KillPersonConditionComponent : Component
{
    /// <summary>
    /// Whether the target must be truly dead, ignores missing evac.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireDead = false;
}
