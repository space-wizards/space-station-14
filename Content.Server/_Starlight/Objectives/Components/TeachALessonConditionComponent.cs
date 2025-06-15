using Content.Server._Starlight.Objectives.Systems;

namespace Content.Server._Starlight.Objectives.Components;

/// <summary>
/// Requires that a target at least dies once.  
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(TeachALessonConditionSystem))]
    
public sealed partial class TeachALessonConditionComponent : Component
{
    /// <summary>
    /// Checks to see if the target has died
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] 
    public bool HasDied = false;

}