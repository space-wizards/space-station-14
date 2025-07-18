using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="DepartmentObjectiveComponent"/> to a random department.
/// </summary>
[RegisterComponent]
public sealed partial class PickRandomDepartmentComponent : Component
{
    [DataField(readOnly: true)] public List<ProtoId<DepartmentPrototype>> Exclude = new (){"Law", "Silicon"};
    [DataField(readOnly: true)] public bool ExcludeNonPrimary = true;
    [DataField(readOnly: true)] public bool ExcludeHidden = true;
}
