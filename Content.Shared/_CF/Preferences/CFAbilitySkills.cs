namespace Content.Shared.CF.Preferences;

/// <summary>
/// The Pathfinder 2e skills (Perception is tracked separately as a proficiency).
/// Lore stands in for every Lore subcategory.
/// Prefer <c>cfAbilitySkill</c> prototype ids in data; this enum mirrors those ids.
/// </summary>
public enum CFAbilitySkill
{
    Acrobatics,
    Arcana,
    Athletics,
    Crafting,
    Deception,
    Diplomacy,
    Intimidation,
    Medicine,
    Nature,
    Occultism,
    Performance,
    Religion,
    Society,
    Stealth,
    Survival,
    Thievery,
    Lore,
}
