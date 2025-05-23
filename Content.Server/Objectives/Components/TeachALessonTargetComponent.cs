namespace Content.Server.Objectives.Components;

/// <summary>
/// 
/// </summary>

[RegisterComponent ]
public sealed partial class TeachALessonTargetComponent : Component
{ 
   [DataField]
   public HashSet<EntityUid> Teachers = new HashSet<EntityUid>();
}