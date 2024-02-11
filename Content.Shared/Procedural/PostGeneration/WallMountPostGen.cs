using Content.Shared.Maps;
using Content.Shared.Storage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns on the boundary tiles of rooms.
/// </summary>
public sealed partial class WallMountPostGen : IPostDunGen
{
    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";

    [DataField("spawns")]
    public List<EntitySpawnEntry> Spawns = new();

    /// <summary>
    /// Chance per free tile to spawn a wallmount.
    /// </summary>
    [DataField("prob")]
    public double Prob = 0.1;
}
