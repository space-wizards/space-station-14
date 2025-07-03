using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Procedural.DungeonLayers;

/// <summary>
/// Samples noise and spawns the specified tile in the dungeon area.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SampleTileDunGen : IDunGenLayer
{
    /// <summary>
    /// Reserve any tiles we update.
    /// </summary>
    [DataField]
    public bool ReserveTiles = true;

    [DataField] public FastNoiseLite Noise { get; private set; } = new(0);

    [DataField]
    public float Threshold { get; private set; } = 0.5f;

    [DataField] public bool Invert { get; private set; } = false;

    /// <summary>
    /// Which tile variants to use for this layer. Uses all of the tile's variants if none specified
    /// </summary>
    [DataField]
    public List<byte>? Variants = null;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile = string.Empty;
}
