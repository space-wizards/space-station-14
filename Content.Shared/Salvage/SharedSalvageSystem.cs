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
        var adjustedSeed = seed % 1;
        return factions[adjustedSeed % factions.Count];
    }

    public static IEnumerable<SalvageLootPrototype> GetLoot(List<string> loots, int seed, IPrototypeManager protoManager)
    {
        for (var i = 0; i < loots.Count; i++)
        {
            var loot = loots[i];
            var a = protoManager.Index<WeightedRandomPrototype>(loot);
            var random = new System.Random(seed % 2 + i);
            var lootConfig = a.Pick(random);
            yield return protoManager.Index<SalvageLootPrototype>(lootConfig);
        }
    }

    public static ISalvageReward GetReward(WeightedRandomPrototype proto, int seed, IPrototypeManager protoManager)
    {
        var rewardProto = proto.Pick(new System.Random(seed % 3));
        return protoManager.Index<SalvageRewardPrototype>(rewardProto).Reward;
    }

    #region Structure

    public static int GetStructureCount(SalvageStructure structure, int seed)
    {
        var adjustedSeed = seed % 4;
        return adjustedSeed % (structure.MaxStructures - structure.MinStructures + 1) + structure.MinStructures;
    }

    #endregion
}
