using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Prototypes;

namespace Content.Server.Procedural;

/// <summary>
/// Spawns the specified biome on top of the dungeon rooms.
/// </summary>
public sealed partial class BiomeLayerPostGen : IPostDunGen
{
    /*
     * TODO:
     * Make the dungeon stuff work with biomes as a once-off load that gets forced and then removed at the very end
     * Add marker layer support too for the thing and add the ability to force it as well.
     * Will require cleaning up biome code.
     */

    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> BiomeTemplate;
}
