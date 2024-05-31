using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

[Prototype("dungeonPreset")]
public sealed partial class DungeonPresetPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// The room pack bounds we need to fill.
    /// </summary>
    [DataField("roomPacks", required: true)]
    public List<Box2i> RoomPacks = new();
}
