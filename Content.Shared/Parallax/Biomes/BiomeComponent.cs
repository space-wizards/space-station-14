using Content.Shared.Parallax.Biomes.Layers;
using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.GameStates;
using Robust.Shared.Noise;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Parallax.Biomes;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedBiomeSystem))]
public sealed partial class BiomeComponent : Component
{
    public FastNoiseLite Noise = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("seed")]
    [AutoNetworkedField]
    public int Seed;

    /// <summary>
    /// The underlying entity, decal, and tile layers for the biome.
    /// </summary>
    [DataField("layers")]
    [AutoNetworkedField]
    public List<IBiomeLayer> Layers = new();

    /// <summary>
    /// Templates to use for <see cref="Layers"/>. Optional as this can be set elsewhere.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("template", customTypeSerializer: typeof(PrototypeIdSerializer<BiomeTemplatePrototype>))]
    public string? Template;

    /// <summary>
    /// If we've already generated a tile and couldn't deload it then we won't ever reload it in future.
    /// Stored by [Chunkorigin, Tiles]
    /// </summary>
    [DataField("modifiedTiles")]
    public Dictionary<Vector2i, HashSet<Vector2i>> ModifiedTiles = new();

    /// <summary>
    /// Decals that have been loaded as a part of this biome.
    /// </summary>
    [DataField("decals")]
    public Dictionary<Vector2i, Dictionary<uint, Vector2i>> LoadedDecals = new();

    [DataField("entities")]
    public Dictionary<Vector2i, List<EntityUid>> LoadedEntities = new();

    /// <summary>
    /// Currently active chunks
    /// </summary>
    [DataField("loadedChunks")]
    public readonly HashSet<Vector2i> LoadedChunks = new();

    #region Markers

    // Alas I cannot use interfaces for indexing, alternatively I could just store types alongside these but that seems
    // subject to issues.
    // We can probably add in dungeon spawner markers but that can be done later.

    /// <summary>
    /// Track what markers we've loaded already to avoid double-loading.
    /// </summary>
    [DataField("loadedMobMarkers", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<HashSet<Vector2i>, BiomeMobMarkerLayerPrototype>))]
    public readonly Dictionary<string, HashSet<Vector2i>> LoadedMobMarkers = new();

    [DataField("mobMarkerLayers", customTypeSerializer: typeof(PrototypeIdListSerializer<BiomeMobMarkerLayerPrototype>))]
    public List<string> MobMarkerLayers = new();

    /// <summary>
    /// Track what markers we've loaded already to avoid double-loading.
    /// </summary>
    [DataField("loadedDungeonMarkers", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<HashSet<Vector2i>, BiomeDungeonMarkerLayerPrototype>))]
    public readonly Dictionary<string, HashSet<Vector2i>> LoadedDungeonMarkers = new();

    [DataField("dungeonMarkerLayers", customTypeSerializer: typeof(PrototypeIdListSerializer<BiomeDungeonMarkerLayerPrototype>))]
    public List<string> DungeonMarkerLayers = new();

    #endregion
}
