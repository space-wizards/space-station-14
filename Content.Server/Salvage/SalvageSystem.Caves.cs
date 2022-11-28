using Robust.Shared.Map.Components;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private SalvageJob GetCaveJob(EntityUid uid, MapGridComponent component, string expedition, int seed)
    {
        // We'll do the CA generation up front as we can do that pretty quickly
        // All of the spawning and other setup we'll run over multiple ticks as it will likely go over.

        return new SalvageJob(seed, SalvageGenTime);
    }
}
