using Content.Shared.Procedural.Loot;
using Content.Shared.Procedural.Rewards;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions.Structure;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage;

public abstract class SharedSalvageSystem : EntitySystem
{
    public static readonly TimeSpan MissionCooldown = TimeSpan.FromSeconds(10);

    public static string GetFaction(List<string> factions, int seed)
    {
        var adjustedSeed = new System.Random(seed + 1);
        return factions[adjustedSeed.Next(factions.Count)];
    }

    public static IEnumerable<SalvageLootPrototype> GetLoot(List<string> loots, int seed, IPrototypeManager protoManager)
    {
        var adjustedSeed = new System.Random(seed + 2);

        for (var i = 0; i < loots.Count; i++)
        {
            var loot = loots[i];
            var a = protoManager.Index<WeightedRandomPrototype>(loot);
            var lootConfig = a.Pick(adjustedSeed);
            yield return protoManager.Index<SalvageLootPrototype>(lootConfig);
        }
    }

    public static ISalvageReward GetReward(WeightedRandomPrototype proto, int seed, IPrototypeManager protoManager)
    {
        var adjustedSeed = new System.Random(seed + 3);
        var rewardProto = proto.Pick(adjustedSeed);
        return protoManager.Index<SalvageRewardPrototype>(rewardProto).Reward;
    }

    #region Structure

    public static int GetStructureCount(SalvageStructure structure, int seed)
    {
        var adjustedSeed = new System.Random(seed + 4);
        return adjustedSeed.Next(structure.MinStructures, structure.MaxStructures + 1);
    }

    #endregion
}
