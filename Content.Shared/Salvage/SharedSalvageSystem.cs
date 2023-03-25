using Content.Shared.Dataset;
using Content.Shared.Procedural.Loot;
using Content.Shared.Procedural.Rewards;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions.Structure;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage;

public abstract class SharedSalvageSystem : EntitySystem
{
    public static readonly TimeSpan MissionCooldown = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MissionFailedCooldown = TimeSpan.FromMinutes(10);

    public static float GetDifficultyModifier(DifficultyRating difficulty)
    {
        // These should reflect how many salvage staff are expected to be required for the mission.
        switch (difficulty)
        {
            case DifficultyRating.None:
                return 1f;
            case DifficultyRating.Minor:
                return 1.5f;
            case DifficultyRating.Moderate:
                return 3f;
            case DifficultyRating.Hazardous:
                return 6f;
            case DifficultyRating.Extreme:
                return 10f;
            default:
                throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
        }
    }

    public static string GetFTLName(DatasetPrototype dataset, int seed)
    {
        var random = new System.Random(seed);
        return $"{dataset.Values[random.Next(dataset.Values.Count)]}-{random.Next(10, 100)}-{(char) (65 + random.Next(26))}";
    }

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
