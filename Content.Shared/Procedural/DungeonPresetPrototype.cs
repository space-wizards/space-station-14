using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

[Prototype("dungeonPreset")]
public sealed class DungeonPresetPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField("roomPacks", required: true)]
    public List<Box2i> RoomPacks = new();
}
