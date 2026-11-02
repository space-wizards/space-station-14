using System.Linq;
using Content.Shared.CF.Preferences;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Character;

/// <summary>
/// Pathfinder 2e attribute / HP / AC / save calculation for character creation and spawn.
/// Attribute modifiers start at +0; each boost is +1 and each flaw is -1.
/// </summary>
public static class CFAttributeMath
{
    public const int LevelOneMaxModifier = 4;
    public const int FinishingFreeBoostCount = 4;
    public const int TrainedProficiencyBonus = 2;

    public static Dictionary<CFAbility, int> EmptyModifiers()
    {
        return Enum.GetValues<CFAbility>().ToDictionary(ability => ability, _ => 0);
    }

    public static int EffectiveSpeed(CFAncestryPrototype? ancestry, CFHeritagePrototype? heritage)
    {
        if (heritage is { Speed: > 0 })
            return heritage.Speed;

        return ancestry?.Speed ?? 25;
    }

    public static CFSize EffectiveSize(CFAncestryPrototype? ancestry, CFHeritagePrototype? heritage)
    {
        if (heritage?.Size is { } size)
            return size;

        return ancestry?.Size ?? CFSize.Medium;
    }

    public static bool HasDarkvision(CFAncestryPrototype? ancestry, CFHeritagePrototype? heritage)
    {
        if (ancestry?.Darkvision == true || heritage?.Darkvision == true)
            return true;

        if (heritage?.UpgradeVision != true || ancestry == null)
            return false;

        return ancestry.Darkvision || ancestry.Traits.Any(trait => trait.Id == "cf-trait-low-light-vision");
    }

    public static CFAncestryPrototype? FindAncestry(IPrototypeManager prototypes, ProtoId<SpeciesPrototype> species)
    {
        foreach (var ancestry in prototypes.EnumeratePrototypes<CFAncestryPrototype>())
        {
            if (ancestry.OwnsSpecies(species))
                return ancestry;
        }

        return null;
    }

    /// <summary>
    /// Background prototypes list candidate abilities for the restricted boost (pick one) plus an implicit free boost.
    /// </summary>
    public static List<CFAbility> BackgroundBoostChoices(CFBackgroundPrototype background)
    {
        var choices = new List<CFAbility>();
        foreach (var boost in background.AbilityBoosts)
        {
            if (boost == CFAbilityBoost.Free)
                continue;

            var ability = (CFAbility)boost;
            if (!choices.Contains(ability))
                choices.Add(ability);
        }

        return choices;
    }

    public static int CountAncestryFreeBoosts(CFAncestryPrototype ancestry)
    {
        var count = 0;
        foreach (var boost in ancestry.AbilityBoosts)
        {
            if (boost == CFAbilityBoost.Free)
                count++;
        }

        return count;
    }

    public static Dictionary<CFAbility, int> ComputeModifiers(
        IPrototypeManager prototypes,
        ProtoId<SpeciesPrototype> species,
        CFCharacterBuildData build)
    {
        var mods = EmptyModifiers();
        var ancestry = FindAncestry(prototypes, species);

        if (ancestry != null)
        {
            foreach (var boost in ancestry.AbilityBoosts)
            {
                if (boost == CFAbilityBoost.Free)
                    continue;

                ApplyBoost(mods, (CFAbility)boost);
            }

            foreach (var flaw in ancestry.AbilityFlaws)
                mods[flaw] -= 1;

            foreach (var free in build.AncestryFreeBoosts)
                ApplyBoost(mods, free);
        }

        if (build.Background != null && prototypes.TryIndex(build.Background.Value, out CFBackgroundPrototype? background))
        {
            if (build.BackgroundBoost is { } choice)
                ApplyBoost(mods, choice);

            if (build.BackgroundFreeBoost is { } free)
                ApplyBoost(mods, free);
        }

        if (build.Class != null && prototypes.TryIndex(build.Class.Value, out CFClassPrototype? cls))
            ApplyBoost(mods, cls.KeyAbility);

        foreach (var free in build.FreeBoosts)
            ApplyBoost(mods, free);

        return mods;
    }

    public static int ComputeMaxHp(
        IPrototypeManager prototypes,
        ProtoId<SpeciesPrototype> species,
        CFCharacterBuildData build,
        IReadOnlyDictionary<CFAbility, int> mods)
    {
        var ancestry = FindAncestry(prototypes, species);
        var ancestryHp = ancestry?.HitPoints ?? 0;
        if (build.Heritage != null
            && prototypes.TryIndex(build.Heritage.Value, out CFHeritagePrototype? heritage)
            && heritage.HitPoints > 0
            && heritage.Fits(ancestry?.ID))
            ancestryHp = heritage.HitPoints;
        var classHp = 0;
        if (build.Class != null && prototypes.TryIndex(build.Class.Value, out CFClassPrototype? cls))
            classHp = cls.HitPoints;

        var hp = ancestryHp + classHp + mods[CFAbility.Constitution];
        return Math.Max(1, hp);
    }

    public static int ComputeUnarmoredAc(IReadOnlyDictionary<CFAbility, int> mods, int level, bool trainedUnarmored)
    {
        return ComputeAc(mods, level, trainedUnarmored, itemBonus: 0, dexCap: null);
    }

    /// <summary>
    /// AC = 10 + Dex (capped) + proficiency + armor item bonus.
    /// </summary>
    public static int ComputeAc(
        IReadOnlyDictionary<CFAbility, int> mods,
        int level,
        bool trained,
        int itemBonus,
        int? dexCap)
    {
        var proficiency = trained ? level + TrainedProficiencyBonus : 0;
        var dex = mods[CFAbility.Dexterity];
        if (dexCap is { } cap)
            dex = Math.Min(dex, cap);

        return 10 + dex + proficiency + itemBonus;
    }

    public static bool IsArmorTrained(CFClassPrototype? cls, string proficiency)
    {
        if (cls == null)
            return false;

        var defenses = cls.Proficiencies.Defenses;
        return defenses.Trained.Contains(proficiency)
               || defenses.Expert.Contains(proficiency)
               || defenses.Master.Contains(proficiency)
               || defenses.Legendary.Contains(proficiency);
    }

    public static int ComputeSave(
        IReadOnlyDictionary<CFAbility, int> mods,
        CFAbility ability,
        int level,
        bool trained)
    {
        var proficiency = trained ? level + TrainedProficiencyBonus : 0;
        return mods[ability] + proficiency;
    }

    public static bool IsSaveTrained(CFClassPrototype? cls, string saveName)
    {
        if (cls == null)
            return false;

        return cls.Proficiencies.SavingThrows.Trained.Contains(saveName)
               || cls.Proficiencies.SavingThrows.Expert.Contains(saveName)
               || cls.Proficiencies.SavingThrows.Master.Contains(saveName)
               || cls.Proficiencies.SavingThrows.Legendary.Contains(saveName);
    }

    public static bool IsUnarmoredTrained(CFClassPrototype? cls)
    {
        if (cls == null)
            return false;

        var defenses = cls.Proficiencies.Defenses;
        return defenses.Trained.Contains("Unarmored")
               || defenses.Expert.Contains("Unarmored")
               || defenses.Master.Contains("Unarmored")
               || defenses.Legendary.Contains("Unarmored");
    }

    public static bool IsWeaponTrained(CFClassPrototype? cls, string proficiency)
    {
        if (cls == null)
            return false;

        var attacks = cls.Proficiencies.Attacks;
        return attacks.Trained.Contains(proficiency)
               || attacks.Expert.Contains(proficiency)
               || attacks.Master.Contains(proficiency)
               || attacks.Legendary.Contains(proficiency);
    }

    public static int ProficiencyBonus(int level, bool trained)
    {
        return trained ? level + TrainedProficiencyBonus : 0;
    }

    public static CFCharacterSheet Snapshot(IPrototypeManager prototypes, HumanoidCharacterProfile profile)
    {
        var build = profile.CFBuild;
        var mods = ComputeModifiers(prototypes, profile.Species, build);
        CFClassPrototype? cls = null;
        if (build.Class != null)
            prototypes.TryIndex(build.Class.Value, out cls);

        var level = Math.Max(1, build.Level);
        var trainedUnarmored = IsUnarmoredTrained(cls);

        return new CFCharacterSheet
        {
            Modifiers = mods,
            MaxHp = ComputeMaxHp(prototypes, profile.Species, build, mods),
            ArmorClass = ComputeUnarmoredAc(mods, level, trainedUnarmored),
            Fortitude = ComputeSave(mods, CFAbility.Constitution, level, IsSaveTrained(cls, "Fortitude")),
            Reflex = ComputeSave(mods, CFAbility.Dexterity, level, IsSaveTrained(cls, "Reflex")),
            Will = ComputeSave(mods, CFAbility.Wisdom, level, IsSaveTrained(cls, "Will")),
            Level = level,
            Class = cls,
            Ancestry = FindAncestry(prototypes, profile.Species),
        };
    }

    private static void ApplyBoost(Dictionary<CFAbility, int> mods, CFAbility ability)
    {
        // Level-1 soft cap: never raise above +4 via a boost.
        if (mods[ability] >= LevelOneMaxModifier)
            return;

        mods[ability] += 1;
    }
}

public sealed class CFCharacterSheet
{
    public required Dictionary<CFAbility, int> Modifiers { get; init; }
    public int MaxHp { get; init; }
    public int ArmorClass { get; init; }
    public int Fortitude { get; init; }
    public int Reflex { get; init; }
    public int Will { get; init; }
    public int Level { get; init; }
    public CFClassPrototype? Class { get; init; }
    public CFAncestryPrototype? Ancestry { get; init; }
}
