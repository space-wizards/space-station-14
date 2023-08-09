using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed class TileSpawnAnomalyComponent : Component
{
    /// <summary>
    /// The maximum radius of tiles scales with stability
    /// </summary>
    [DataField("spawnRange"), ViewVariables(VVAccess.ReadWrite)]
    public float SpawnRange = 5f;

    /// <summary>
    /// The tile that is spawned by the anomaly's effect
    /// </summary>
    [DataField("floorTileId", customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>)), ViewVariables(VVAccess.ReadWrite)]
    public string FloorTileId = "FloorFlesh";
}
