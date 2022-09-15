using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Roles;

[Prototype("department")]
public sealed class DepartmentPrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("roles", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string> Roles = new();
}
