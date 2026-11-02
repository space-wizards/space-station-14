using Robust.Shared.Serialization;

namespace Content.Shared.CF.Preferences;

/// <summary>
///     The six ability scores in a Character, Represents a character ability, such as Strength or Dexterity.
/// </summary>

[Serializable, NetSerializable]
public enum CFAbility
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma
}

/// <summary>
/// An ability-score boost. <see cref="Free"/> means the player chooses any score.
/// </summary>
[Serializable, NetSerializable]
public enum CFAbilityBoost
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma,
    Free
}