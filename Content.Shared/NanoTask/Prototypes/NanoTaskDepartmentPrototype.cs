using Content.Shared.Access;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Prototypes;

namespace Content.Shared.NanoTask.Prototypes;

[Prototype]
public sealed class NanoTaskDepartmentPrototype : IPrototype, IAccessReader
{
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField]
    public LocId Name { get; set; }

    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> Access = [];

    public HashSet<ProtoId<AccessLevelPrototype>> GetAccesses() => Access;
}
