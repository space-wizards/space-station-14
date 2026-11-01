using System.Linq;
using Content.Shared.CF.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Character;

/// <summary>PF2e proficiency ranks and the flat bonus they add (before level).</summary>
public enum CFProficiencyRank : byte
{
    Untrained = 0,
    Trained = 2,
    Expert = 4,
    Master = 6,
    Legendary = 8,
}

/// <summary>Skill / Perception check math for Pathfinder character sheets.</summary>
public static class CFSkills
{
    /// <summary>
    /// Sheet skill columns (user layout). Thievery is appended to column 3.
    /// </summary>
    public static readonly string[] Column1 =
    [
        "Acrobatics",
        "Crafting",
        "Intimidation",
        "Occultism",
        "Society",
        "Lore",
    ];

    public static readonly string[] Column2 =
    [
        "Arcana",
        "Deception",
        "Medicine",
        "Performance",
        "Survival",
    ];

    public static readonly string[] Column3 =
    [
        "Athletics",
        "Diplomacy",
        "Nature",
        "Religion",
        "Stealth",
        "Thievery",
    ];

    public static int ProficiencyBonus(int level, CFProficiencyRank rank)
    {
        if (rank == CFProficiencyRank.Untrained)
            return 0;

        return level + (int)rank;
    }

    public static CFProficiencyRank RankFromList(CFProficiencyList list, string id)
    {
        if (list.Legendary.Contains(id))
            return CFProficiencyRank.Legendary;
        if (list.Master.Contains(id))
            return CFProficiencyRank.Master;
        if (list.Expert.Contains(id))
            return CFProficiencyRank.Expert;
        if (list.Trained.Contains(id))
            return CFProficiencyRank.Trained;
        return CFProficiencyRank.Untrained;
    }

    public static CFProficiencyRank GetSkillRank(
        CFClassPrototype? cls,
        CFBackgroundPrototype? background,
        string skillId,
        IReadOnlyCollection<string>? trainedSkills = null)
    {
        var rank = CFProficiencyRank.Untrained;
        if (cls != null)
            rank = RankFromList(cls.Proficiencies.Skills, skillId);

        if (rank != CFProficiencyRank.Untrained)
            return rank;

        if (trainedSkills != null)
            return trainedSkills.Contains(skillId) ? CFProficiencyRank.Trained : CFProficiencyRank.Untrained;

        // Fallback when no explicit picks (NPC / incomplete build): prototype background lists.
        if (background != null
            && (background.TrainedSkills.Any(s => s.Id == skillId)
                || background.SkillBoosts.Any(s => s.Id == skillId)))
        {
            return CFProficiencyRank.Trained;
        }

        return CFProficiencyRank.Untrained;
    }

    /// <summary>
    /// Background + free class skill picks for a build (class fixed skills stay on the class list).
    /// </summary>
    public static List<CFFeatPrototype> SelectedFeats(
        IPrototypeManager prototypes,
        CFCharacterBuildData build,
        string? ancestryId,
        string? classId)
    {
        var result = new List<CFFeatPrototype>();
        AddFeat(prototypes, result, build.AncestryFeat, feat => feat.Fits(ancestryId, build.Level));
        AddFeat(prototypes, result, build.ClassFeat, feat => feat.FitsClass(classId, 1));
        AddFeat(prototypes, result, build.ClassFeatLevel2, feat => feat.FitsClass(classId, 2));
        return result;
    }

    private static void AddFeat(
        IPrototypeManager prototypes,
        List<CFFeatPrototype> result,
        ProtoId<CFFeatPrototype>? id,
        Func<CFFeatPrototype, bool> fits)
    {
        if (id != null
            && prototypes.TryIndex(id.Value, out CFFeatPrototype? feat)
            && fits(feat))
            result.Add(feat);
    }

    public static List<string> CollectTrainedSkills(
        IPrototypeManager prototypes,
        CFCharacterBuildData build,
        string? ancestryId = null,
        string? classId = null)
    {
        var result = new List<string>();

        if (build.BackgroundTrainedSkills.Count > 0)
        {
            foreach (var skill in build.BackgroundTrainedSkills)
                result.Add(skill.Id);
        }
        else if (build.Background != null
                 && prototypes.TryIndex(build.Background.Value, out CFBackgroundPrototype? background))
        {
            foreach (var skill in BackgroundSkillSlots(background))
                result.Add(skill.Id);
        }

        foreach (var skill in build.ClassTrainedSkills)
            result.Add(skill.Id);

        foreach (var feat in SelectedFeats(prototypes, build, ancestryId, classId))
        {
            foreach (var skill in feat.TrainedSkills)
                result.Add(skill.Id);
        }

        return result.Distinct(StringComparer.Ordinal).ToList();
    }

    public static List<ProtoId<CFAbilitySkillPrototype>> BackgroundSkillSlots(CFBackgroundPrototype background)
    {
        return background.SkillBoosts
            .Concat(background.TrainedSkills)
            .Distinct()
            .ToList();
    }

    public static int ClassSkillSlotCount(
        CFClassPrototype cls,
        IReadOnlyDictionary<CFAbility, int> abilityModifiers)
    {
        var intMod = Math.Max(0, abilityModifiers.GetValueOrDefault(CFAbility.Intelligence));
        return Math.Max(0, cls.AdditionalSkillTrainings + intMod);
    }

    public static CFProficiencyRank GetSaveRank(CFClassPrototype? cls, string saveName)
    {
        return cls == null
            ? CFProficiencyRank.Untrained
            : RankFromList(cls.Proficiencies.SavingThrows, saveName);
    }

    public static CFProficiencyRank GetPerceptionRank(CFClassPrototype? cls)
    {
        return cls == null
            ? CFProficiencyRank.Untrained
            : RankFromList(cls.Proficiencies.Perception, "Perception");
    }

    public static int SkillModifier(
        IReadOnlyDictionary<CFAbility, int> mods,
        CFAbility ability,
        int level,
        CFProficiencyRank rank,
        int itemBonus = 0)
    {
        return mods.GetValueOrDefault(ability) + ProficiencyBonus(level, rank) + itemBonus;
    }

    public static int AbilityScoreFromModifier(int modifier) => 10 + modifier * 2;

    public static string FormatSigned(int value) => value >= 0 ? $"+{value}" : value.ToString();
}
