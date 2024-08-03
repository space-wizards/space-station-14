using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns the specified marker layer on top of the dungeon rooms.
/// </summary>
public sealed partial class BiomeMarkerLayerDunGen : IDunGenLayer
{
    /// <summary>
    /// How many times to spawn marker layers; can duplicate.
    /// </summary>
    [DataField]
    public int Count = 6;

    [DataField(required: true)]
    public ProtoId<WeightedRandomPrototype> MarkerTemplate;
}
