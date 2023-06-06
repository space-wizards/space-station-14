using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.DungeonGenerators;

public sealed class BSPDunGen : IDunGen
{
    /// <summary>
    /// Rooms need to match any of these tags
    /// </summary>
    [DataField("roomWhitelist", customTypeSerializer:typeof(PrototypeIdListSerializer<TagPrototype>))]
    public List<string> RoomWhitelist = new();

    /// <summary>
    /// Fallback tile.
    /// </summary>
    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";
}
