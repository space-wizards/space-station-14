using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the changeling:
/// 1. Is on the emergency shuttle when docking to CentComm
/// 2. Is currently transformed into the target identity
/// 3. Is wearing an ID card with the target's name
/// Depends on <see cref="TargetObjectiveComponent"/> to know which target identity to check.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingEscapeDepartmentConditionSystem))]
public sealed partial class ChangelingEscapeDepartmentConditionComponent : Component
{
    /// <summary>
    /// The department that is targeted by the objective.
    /// </summary>
    [DataField]
    public ProtoId<DepartmentPrototype> Department;
}
