using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

[Prototype]
public sealed partial class DungeonConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public IDunGen Generator = default!;

    /// <summary>
    /// Ran after the main dungeon is created.
    /// </summary>
    [DataField]
    public List<IPostDunGen> PostGeneration = new();

    /// <summary>
    /// Minimum amount we can offset the dungeon by.
    /// </summary>
    [DataField]
    public int MinOffset;

    /// <summary>
    /// Maximum amount we can offset the dungeon by.
    /// </summary>
    [DataField]
    public int MaxOffset;
}
