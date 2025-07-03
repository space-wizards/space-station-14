using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.DungeonLayers;

/// <summary>
/// Samples noise to spawn the specified entity
/// </summary>
public sealed partial class SampleEntityDunGen : IDunGenLayer
{
    /// <summary>
    /// Reserve any tiles we update.
    /// </summary>
    [DataField]
    public bool ReserveTiles = true;

    [DataField(customTypeSerializer:typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> AllowedTiles { get; private set; } = new();

    [DataField] public FastNoiseLite Noise { get; private set; } = new(0);

    [DataField]
    public float Threshold { get; private set; } = 0.5f;

    [DataField] public bool Invert { get; private set; } = false;

    [DataField]
    public List<EntProtoId> Entities = new();
}
