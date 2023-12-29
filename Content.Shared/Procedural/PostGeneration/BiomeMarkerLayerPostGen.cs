using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns the specified marker layer on top of the dungeon rooms.
/// </summary>
public sealed partial class BiomeMarkerLayerPostGen : IPostDunGen
{
    /// <summary>
    /// Overwrites the marker layer count if > 0.
    /// </summary>
    [DataField]
    public int Count = 0;

    [DataField(required: true)]
    public ProtoId<BiomeMarkerLayerPrototype> MarkerTemplate;
}
