using Content.Server.DeltaV.Objectives.Systems;
using Content.Server.Objectives.Components;

namespace Content.Server.DeltaV.Objectives.Components;

/// <summary>
/// Requires that a target dies once and only once.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(TeachLessonConditionSystem))]
public sealed partial class TeachLessonConditionComponent : Component;
