using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the changeling:
/// 1. Is on the emergency shuttle when docking to CentComm
/// 2. Is currently transformed into the identity of someone in the target department
/// 3. Is wearing an ID card with the matching dept
/// Depends on <see cref="TargetObjectiveComponent"/> to know which target department to check for.
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
