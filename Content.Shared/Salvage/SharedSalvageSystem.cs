using Content.Shared.Salvage.Expeditions.Structure;

namespace Content.Shared.Salvage;

public abstract class SharedSalvageSystem : EntitySystem
{
    public static readonly TimeSpan MissionCooldown = TimeSpan.FromSeconds(10);

    public static string GetBiome(List<string> biomes, int seed)
    {
        var adjustedSeed = seed % 3;
        return biomes[adjustedSeed % biomes.Count];
    }

    public static string GetFaction(List<string> factions, int seed)
    {
        var adjustedSeed = seed % 4;
        return factions[adjustedSeed % factions.Count];
    }

    #region Structure

    public static int GetStructureCount(SalvageStructure structure, int seed)
    {
        var adjustedSeed = seed % 2;
        return adjustedSeed % (structure.MaxStructures - structure.MinStructures + 1) + structure.MinStructures;
    }

    #endregion
}
