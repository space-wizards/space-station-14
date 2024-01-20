using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed partial class TileSpawnAnomalyComponent : Component
{
    /// <summary>
    /// The maximum radius of tiles scales with stability
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpawnRange = 5f;

    /// <summary>
    /// The probability a tile will spawn.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpawnChance = 0.33f;

    /// <summary>
    /// The tile that is spawned by the anomaly's effect
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ContentTileDefinition> FloorTileId = "FloorFlesh";
}
