using Content.Shared.Parallax.Biomes;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    /*
     * Urgh I guess just make biome layers prototypes
     * Then I just need to be able to slot a layer in on top of a marker.
     * e.g. (AddLayer(biomecomp, biomelayerprototype, index)
     *
     */

    /*
     * SO
     * 1. Add markers to be run before / after everything else
     * 2. Port biome layers to prototypes
     * 3. Add support for dynamic layers.
     */

    // Okay so

    private void InitializePoints()
    {

    }
}
