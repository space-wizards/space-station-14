using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Contraband.Prototypes;

[Prototype]
public sealed partial class ContrabandCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = "";
    [DataField("allowedDeps")]
    public HashSet<ProtoId<DepartmentPrototype>> AllowedDepartments = new();
}
