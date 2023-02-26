using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Procedural;

[Prototype("dungeonRoom")]
public sealed class DungeonRoomPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField("size", required: true)] public Vector2i Size;

    /// <summary>
    /// Path to the file to use for the room.
    /// </summary>
    [DataField("atlas", required: true)] public ResourcePath AtlasPath = default!;

    /// <summary>
    /// Tile offset into the atlas to use for the room.
    /// </summary>
    [DataField("offset", required: true)] public Vector2i Offset;
}
