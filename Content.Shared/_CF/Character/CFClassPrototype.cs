using Content.Shared.CF.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Character;

[Prototype("cfClass")]
public sealed partial class CFClassPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public int HitPoints;

    [DataField(required: true)]
    public CFAbility KeyAbility;

    [DataField(required: true)]
    public CFAbility HitPointsAbility;

    [DataField(required: true)]
    public LocId ClassDC;

    [DataField]
    public CFClassProficiencies Proficiencies = new();

    [DataField]
    public List<CFAbilityBoost> AbilityBoosts = new();

    /// <summary>
    /// Free skill trainings at level 1, before Intelligence modifier
    /// (final slots = this + max(0, Int mod)).
    /// </summary>
    [DataField]
    public int AdditionalSkillTrainings;

    [DataField]
    public CFSpellcasting? Spellcasting;

    /// <summary>Class features from level 1 through 5.</summary>
    [DataField]
    public List<CFClassFeature> Features = new();

    [DataField]
    public CFRarity Rarity = CFRarity.Common;
}

/// <summary>One class feature gained by the given level. Later levels are not imported.</summary>
[DataDefinition]
public sealed partial class CFClassFeature
{
    [DataField]
    public int Level = 1;

    [DataField(required: true)]
    public string Name { get; private set; } = "";

    [DataField]
    public string Description { get; private set; } = "";
}

/// <summary>
/// Prepared or spontaneous spell slots for a class. Rank 1 is <see cref="Slots"/> index 0.
/// </summary>
[DataDefinition]
public sealed partial class CFSpellcasting
{
    [DataField(required: true)]
    public CFSpellTradition Tradition;

    [DataField(required: true)]
    public CFAbility Ability;

    [DataField]
    public int Cantrips = 5;

    [DataField]
    public List<int> Slots = new();

    [DataField]
    public int FocusPoints;

    /// <summary>
    /// Prepared casters fill their slots from the character-sheet preparation window.
    /// Spontaneous casters cast the spells they know.
    /// </summary>
    [DataField]
    public bool Prepared;
}

[DataDefinition]
public sealed partial class CFClassProficiencies
{
    [DataField]
    public CFProficiencyList Perception = new();

    [DataField]
    public CFProficiencyList SavingThrows = new();

    [DataField]
    public CFProficiencyList Skills = new();

    [DataField]
    public CFProficiencyList Attacks = new();

    [DataField]
    public CFProficiencyList Defenses = new();
}

[DataDefinition]
public sealed partial class CFProficiencyList
{
    [DataField]
    public List<string> Trained = new();

    [DataField]
    public List<string> Expert = new();

    [DataField]
    public List<string> Master = new();

    [DataField]
    public List<string> Legendary = new();
}
