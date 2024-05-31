using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Procedural;

[Prototype("dungeonRoom")]
public sealed partial class DungeonRoomPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField("tags", customTypeSerializer:typeof(PrototypeIdListSerializer<TagPrototype>))]
    public List<string> Tags = new();

    [DataField("size", required: true)] public Vector2i Size;

    /// <summary>
    /// Path to the file to use for the room.
    /// </summary>
    [DataField("atlas", required: true)] public ResPath AtlasPath;

    /// <summary>
    /// Tile offset into the atlas to use for the room.
    /// </summary>
    [DataField("offset", required: true)] public Vector2i Offset;
}
