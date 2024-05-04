using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Recursive dungeon generator
/// </summary>
public sealed partial class GroupDunGen : IDunGen
{
    [DataField(required: true)]
    public List<ProtoId<DungeonConfigPrototype>> Configs = new();
}
