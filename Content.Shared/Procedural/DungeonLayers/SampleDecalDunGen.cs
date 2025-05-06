using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.DungeonLayers;

public sealed partial class SampleDecalDunGen : IDunGenLayer
{
    /// <summary>
    /// Reserve any tiles we update.
    /// </summary>
    [DataField]
    public bool ReserveTiles = true;

    [DataField(customTypeSerializer:typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> AllowedTiles { get; private set; } = new();

    /// <summary>
    /// Divide each tile up by this amount.
    /// </summary>
    [DataField]
    public float Divisions = 1f;

    [DataField]
    public FastNoiseLite Noise { get; private set; } = new(0);

    [DataField]
    public float Threshold { get; private set; } = 0.8f;

    [DataField] public bool Invert { get; private set; } = false;

    [DataField(required: true)]
    public List<ProtoId<DecalPrototype>> Decals = new();
}
