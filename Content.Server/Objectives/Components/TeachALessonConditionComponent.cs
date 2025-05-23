using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target dies once, 
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(TeachALessonConditionSystem))]
    
public sealed partial class TeachALessonConditionComponent : Component
{
    /// <summary>
    /// Whether the target must be truly dead, ignores missing evac.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireDead = false;

    /// <summary>
    /// Checks to see if the target has died
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] 
    public bool HasDied = false;

}