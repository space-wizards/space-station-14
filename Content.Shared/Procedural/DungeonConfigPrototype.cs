using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

[Prototype("dungeonConfig")]
public sealed partial class DungeonConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("generator", required: true)]
    public IDunGen Generator = default!;

    /// <summary>
    /// Ran after the main dungeon is created.
    /// </summary>
    [DataField("postGeneration")]
    public List<IPostDunGen> PostGeneration = new();
}
