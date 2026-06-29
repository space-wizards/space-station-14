using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared.Shuttles.Prototypes;

[Prototype]
public sealed partial class DungeonPackPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<ProtoId<DungeonConfigPrototype>> DungeonConfigs { get; private set; } =  new();
}
