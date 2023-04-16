using System.Linq;
using Content.Shared.Dataset;
using Content.Shared.Procedural.Loot;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Salvage;

public abstract class SharedSalvageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public static readonly TimeSpan MissionCooldown = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MissionFailedCooldown = TimeSpan.FromMinutes(15);

    public int GetDifficulty(DifficultyRating rating)
    {
        switch (rating)
        {
            case DifficultyRating.None:
                return 1;
            case DifficultyRating.Minor:
                return 2;
            case DifficultyRating.Moderate:
                return 4;
            case DifficultyRating.Hazardous:
                return 7;
            case DifficultyRating.Extreme:
                return 11;
            default:
                throw new ArgumentOutOfRangeException(nameof(rating), rating, null);
        }
    }

    public static string GetFTLName(DatasetPrototype dataset, int seed)
    {
        var random = new System.Random(seed);
        return $"{dataset.Values[random.Next(dataset.Values.Count)]}-{random.Next(10, 100)}-{(char) (65 + random.Next(26))}";
    }

    public SalvageMission GetMission(string config, DifficultyRating difficulty, int seed)
    {
        // This is on shared to ensure the client display for missions and what the server generates are consistent
        var rating = (float) GetDifficulty(difficulty);
        var rand = new System.Random(seed);
        var faction = GetMod<SalvageFactionPrototype>(rand, ref rating);
        var biome = GetMod<SalvageBiomeMod>(rand, ref rating);

        var dungeon = GetDungeon(biome.ID, rand, ref rating);

        SalvageLightMod? light = null;

        if (biome.BiomePrototype != null)
        {
            light = GetLight(biome.ID, rand, ref rating);
        }

        var time = GetMod<SalvageTimeMod>(rand, ref rating);

        if (rand.Prob(0.2f))
        {
            var weather = GetMod<SalvageWeatherMod>(rand, ref rating);
        }

        var exactDuration = time.MinDuration + (time.MaxDuration - time.MinDuration) * rand.NextFloat();
        exactDuration = MathF.Round(exactDuration / 15f) * 15f;

        var duration = TimeSpan.FromSeconds(exactDuration);

        return new SalvageMission(seed, dungeon.ID, faction.ID, config, biome.BiomePrototype, light?.Color, duration);
    }

    public SalvageDungeonMod GetDungeon(string biome, System.Random rand, ref float rating)
    {
        var mods = _proto.EnumeratePrototypes<SalvageDungeonMod>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        foreach (var mod in mods)
        {
            if (mod.BiomeMods?.Contains(biome) == false ||
                mod.Cost > rating)
            {
                continue;
            }

            rating -= (int) mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
    }

    public SalvageLightMod GetLight(string biome, System.Random rand, ref float rating)
    {
        var mods = _proto.EnumeratePrototypes<SalvageLightMod>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        foreach (var mod in mods)
        {
            if (mod.Biomes?.Contains(biome) == false || mod.Cost > rating)
                continue;

            rating -= (int) mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
    }

    public T GetMod<T>(System.Random rand, ref float rating) where T : class, IPrototype, ISalvageMod
    {
        var mods = _proto.EnumeratePrototypes<T>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        foreach (var mod in mods)
        {
            if (mod.Cost > rating)
                continue;

            rating -= (int) mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
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
}
