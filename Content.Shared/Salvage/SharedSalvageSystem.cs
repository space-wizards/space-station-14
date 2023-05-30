using System.Linq;
using Content.Shared.Dataset;
using Content.Shared.Procedural.Loot;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Salvage;

public abstract class SharedSalvageSystem : EntitySystem
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    #region Descriptions

    public string GetMissionDescription(SalvageMission mission)
    {
        // Hardcoded in coooooz it's dynamic based on difficulty and I'm lazy.
        switch (mission.Mission)
        {
            case SalvageMissionType.Mining:
                // Taxation: , ("tax", $"{GetMiningTax(mission.Difficulty) * 100f:0}")
                return Loc.GetString("salvage-expedition-desc-mining");
            case SalvageMissionType.Destruction:
                var proto = _proto.Index<SalvageFactionPrototype>(mission.Faction).Configs["DefenseStructure"];

                return Loc.GetString("salvage-expedition-desc-structure",
                    ("count", GetStructureCount(mission.Difficulty)),
                    ("structure", _loc.GetEntityData(proto).Name));
            case SalvageMissionType.Elimination:
                return Loc.GetString("salvage-expedition-desc-elimination");
            default:
                throw new NotImplementedException();
        }
    }

    public float GetMiningTax(DifficultyRating baseRating)
    {
        return 0.6f + (int) baseRating * 0.05f;
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
            case DifficultyRating.Minimal:
                return 1;
            case DifficultyRating.Minor:
                return 2;
            case DifficultyRating.Moderate:
                return 4;
            case DifficultyRating.Hazardous:
                return 6;
            case DifficultyRating.Extreme:
                return 8;
            default:
                throw new ArgumentOutOfRangeException(nameof(rating), rating, null);
        }
    }

    /// <summary>
    /// How many groups of mobs to spawn for a mission.
    /// </summary>
    public float GetSpawnCount(DifficultyRating difficulty)
    {
        return (int) difficulty * 2;
    }

    public static string GetFTLName(DatasetPrototype dataset, int seed)
    {
        var random = new System.Random(seed);
        return $"{dataset.Values[random.Next(dataset.Values.Count)]}-{random.Next(10, 100)}-{(char) (65 + random.Next(26))}";
    }

    public SalvageMission GetMission(SalvageMissionType config, DifficultyRating difficulty, int seed)
    {
        // This is on shared to ensure the client display for missions and what the server generates are consistent
        var rating = (float) GetDifficulty(difficulty);
        // Don't want easy missions to have any negative modifiers but also want
        // easy to be a 1 for difficulty.
        rating -= 1f;
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

        var loots = GetLoot(config, _proto.EnumeratePrototypes<SalvageLootPrototype>().ToList(), GetDifficulty(difficulty), seed);
        return new SalvageMission(seed, difficulty, dungeon.ID, faction.ID, config, biome.ID, light?.Color, duration, loots, mods);
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

    private Dictionary<string, int> GetLoot(SalvageMissionType mission, List<SalvageLootPrototype> loots, int count, int seed)
    {
        var results = new Dictionary<string, int>();
        var adjustedSeed = new System.Random(seed + 2);

        for (var i = 0; i < count; i++)
        {
            adjustedSeed.Shuffle(loots);

            foreach (var loot in loots)
            {
                if (loot.Blacklist.Contains(mission))
                    continue;

                var weh = results.GetOrNew(loot.ID);
                weh++;
                results[loot.ID] = weh;
                break;
            }
        }

        return results;
    }
}

[Serializable, NetSerializable]
public enum SalvageMissionType : byte
{
    /// <summary>
    /// No dungeon, just ore loot and random mob spawns.
    /// </summary>
    Mining,

    /// <summary>
    /// Destroy the specified structures in a dungeon.
    /// </summary>
    Destruction,

    /// <summary>
    /// Kill a large creature in a dungeon.
    /// </summary>
    Elimination,
}

[Serializable, NetSerializable]
public enum DifficultyRating : byte
{
    Minimal,
    Minor,
    Moderate,
    Hazardous,
    Extreme,
}
