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
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public static readonly TimeSpan MissionCooldown = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MissionFailedCooldown = TimeSpan.FromMinutes(15);

    #region Descriptions

    public string GetMissionDescription(SalvageMission mission)
    {
        // Hardcoded in coooooz it's dynamic based on difficulty and I'm lazy.
        switch (mission.Mission)
        {
            case "Mining":
                return Loc.GetString("salvage-expedition-desc-mining");
            case "StructureDestroy":
                var proto = _proto.Index<SalvageFactionPrototype>(mission.Faction).Configs["DefenseStructure"];

                return Loc.GetString("salvage-expedition-desc-structure",
                    ("count", GetStructureCount(mission.Difficulty)),
                    ("structure", _loc.GetEntityData(proto).Name));
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Gets the amount of structures to destroy.
    /// </summary>
    public int GetStructureCount(DifficultyRating baseRating)
    {
        return 1 + (int) baseRating * 2;
    }

    #endregion

    public int GetDifficulty(DifficultyRating rating)
    {
        switch (rating)
        {
            case DifficultyRating.None:
                return 0;
            case DifficultyRating.Minor:
                return 1;
            case DifficultyRating.Moderate:
                return 3;
            case DifficultyRating.Hazardous:
                return 6;
            case DifficultyRating.Extreme:
                return 10;
            default:
                throw new ArgumentOutOfRangeException(nameof(rating), rating, null);
        }
    }

    /// <summary>
    /// How many groups of mobs to spawn for a mission.
    /// </summary>
    public float GetSpawnCount(DifficultyRating difficulty, float remaining)
    {
        return (int) difficulty * 2 + remaining + 1;
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
        var mods = new List<string>();

        SalvageLightMod? light = null;

        if (biome.BiomePrototype != null)
        {
            light = GetLight(biome.ID, rand, ref rating);
            mods.Add(light.Description);
        }

        var time = GetMod<SalvageTimeMod>(rand, ref rating);
        // Round the duration to nearest 15 seconds.
        var exactDuration = time.MinDuration + (time.MaxDuration - time.MinDuration) * rand.NextFloat();
        exactDuration = MathF.Round(exactDuration / 15f) * 15f;
        var duration = TimeSpan.FromSeconds(exactDuration);

        if (time.ID != "StandardTime")
        {
            mods.Add(time.Description);
        }

        if (rand.Prob(0.2f))
        {
            var weather = GetMod<SalvageWeatherMod>(rand, ref rating);
            // mods.Add(weather.Description);
        }

        var loots = GetLoot(_proto.EnumeratePrototypes<SalvageLootPrototype>().ToList(), GetDifficulty(difficulty), seed);
        rating = MathF.Max(0f, rating);

        return new SalvageMission(seed, difficulty, rating, dungeon.ID, faction.ID, config, biome.ID, light?.Color, duration, loots, mods);
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

            rating -= mod.Cost;

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

            rating -= mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
    }

    public List<string> GetLoot(List<SalvageLootPrototype> loots, int count, int seed)
    {
        var results = new List<string>(count);
        var adjustedSeed = new System.Random(seed + 2);

        for (var i = 0; i < count; i++)
        {
            var loot = loots[adjustedSeed.Next(loots.Count)];
            results.Add(loot.ID);
        }

        return results;
    }
}
