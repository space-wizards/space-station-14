namespace Content.Server._Starlight.Objectives.Components;

/// <summary>
/// Marker component for the target of Teach a lesson Objective
/// Holds HashSet of entities with this objective
/// </summary>

[RegisterComponent ]
public sealed partial class TeachALessonTargetComponent : Component
{ 
   [DataField]
   public HashSet<EntityUid> Teachers = new HashSet<EntityUid>();
}