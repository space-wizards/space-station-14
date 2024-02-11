using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Places rooms in pre-selected pack layouts. Chooses rooms from the specified whitelist.
/// </summary>
public sealed partial class PrefabDunGen : IDunGen
{
    /// <summary>
    /// Rooms need to match any of these tags
    /// </summary>
    [DataField("roomWhitelist", customTypeSerializer:typeof(PrototypeIdListSerializer<TagPrototype>))]
    public List<string> RoomWhitelist = new();

    /// <summary>
    /// Room pack presets we can use for this prefab.
    /// </summary>
    [DataField("presets", required: true, customTypeSerializer:typeof(PrototypeIdListSerializer<DungeonPresetPrototype>))]
    public List<string> Presets = new();

    /// <summary>
    /// Fallback tile.
    /// </summary>
    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";
}
