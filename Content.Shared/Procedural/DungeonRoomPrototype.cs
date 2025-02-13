using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Procedural;

[Prototype("dungeonRoom")]
public sealed partial class DungeonRoomPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    [DataField(required: true)]
    public Vector2i Size;

    /// <summary>
    /// Path to the file to use for the room.
    /// </summary>
    [DataField("atlas", required: true)]
    public ResPath AtlasPath;

    /// <summary>
    /// Tile offset into the atlas to use for the room.
    /// </summary>
    [DataField(required: true)]
    public Vector2i Offset;

    /// <summary>
    /// These tiles will be ignored when copying from the atlas into the actual game,
    /// allowing you to make rooms of irregular shapes that blend seamlessly into their surroundings
    /// </summary>
    [DataField]
    public ProtoId<ContentTileDefinition>? IgnoreTile;
}
