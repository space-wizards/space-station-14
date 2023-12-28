using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Prototypes;

namespace Content.Server.Procedural;

/// <summary>
/// Spawns the specified biome on top of the dungeon rooms.
/// </summary>
public sealed partial class BiomeMarkerLayerPostGen : IPostDunGen
{
    [DataField(required: true)]
    public ProtoId<BiomeMarkerLayerPrototype> MarkerTemplate;
}
