using Content.Shared.CF.Character;
using Content.Shared.CF.Preferences;
using Robust.Shared.Random;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder 2e melee Strike helpers (d20 vs AC, degrees of success, damage dice).
/// </summary>
public static class CFMelee
{
    /// <summary>
    /// Melee reach in tiles. One SS14 tile is ~1.8 m (~6 ft); 1.25 covers adjacent + a little reach.
    /// </summary>
    public const float MeleeRange = 1.25f;

    /// <summary>Default PC / commoner fist Strike (PF2e unarmed): 1d4 bludgeoning.</summary>
    public const int UnarmedDamageDice = 1;
    public const int UnarmedDamageDie = 4;
    public const string UnarmedDamageType = "bludgeoning";
    public const string UnarmedProficiency = "Unarmed";

    /// <summary>Approximate PF feet per SS14 tile (1 tile ≈ 1.5 m ≈ 5 ft grid square).</summary>
    public const float FeetPerTile = 5f;

    /// <summary>
    /// Multiple Attack Penalty for an attack roll. <paramref name="attacksAlreadyMade"/> is how many
    /// attack rolls this creature has made since its action pool was last full.
    /// </summary>
    public static int MapPenalty(int attacksAlreadyMade, bool agile)
    {
        var step = Math.Clamp(attacksAlreadyMade, 0, 2);
        if (agile)
            return step == 0 ? 0 : step == 1 ? -4 : -8;

        return step == 0 ? 0 : step == 1 ? -5 : -10;
    }

    public static bool InReach(float distance) => distance <= MeleeRange;

    public static float TilesToFeet(float tiles) => tiles * FeetPerTile;

    public static int AttackModifier(
        int strengthMod,
        int dexterityMod,
        int proficiencyBonus,
        bool finesse,
        int itemBonus = 0)
    {
        var ability = finesse ? Math.Max(strengthMod, dexterityMod) : strengthMod;
        return ability + proficiencyBonus + itemBonus;
    }

    public static int DamageModifier(int strengthMod, bool finesse, int dexterityMod)
    {
        // Finesse weapons still add Strength to damage unless they have a trait that says otherwise.
        // For MVP, finesse only affects the attack roll; damage always uses Strength.
        _ = finesse;
        _ = dexterityMod;
        return strengthMod;
    }

    public static CFStrikeDegree DegreeOfSuccess(int d20, int attackBonus, int targetAc)
    {
        var total = d20 + attackBonus;
        var degree = total >= targetAc ? CFStrikeDegree.Success : CFStrikeDegree.Failure;

        if (total >= targetAc + 10)
            degree = CFStrikeDegree.CriticalSuccess;
        else if (total <= targetAc - 10)
            degree = CFStrikeDegree.CriticalFailure;

        // Natural 20 / 1 adjust the degree by one step.
        if (d20 == 20)
            degree = Improve(degree);
        else if (d20 == 1)
            degree = Worsen(degree);

        return degree;
    }

    public static int RollDamage(IRobustRandom random, int diceCount, int dieFaces, int damageMod, bool critical)
    {
        var dice = critical ? diceCount * 2 : diceCount;
        var total = damageMod;
        for (var i = 0; i < dice; i++)
            total += random.Next(1, dieFaces + 1);

        return Math.Max(0, total);
    }

    public static int AttackBonusFromSheet(CFCharacterSheet sheet, CFMeleeWeaponComponent weapon)
    {
        var trained = CFAttributeMath.IsWeaponTrained(sheet.Class, weapon.Proficiency);
        var proficiency = CFAttributeMath.ProficiencyBonus(sheet.Level, trained);
        return AttackModifier(
            sheet.Modifiers[CFAbility.Strength],
            sheet.Modifiers[CFAbility.Dexterity],
            proficiency,
            weapon.Finesse,
            weapon.ItemBonus);
    }

    public static int AttackBonusFromCombatant(
        CFAbilityScoreComponent abilities,
        CFCombatantComponent combatant,
        CFMeleeWeaponComponent weapon,
        bool weaponTrained)
    {
        return AttackBonusFromCombatant(
            abilities,
            combatant,
            weapon.Finesse,
            weapon.ItemBonus,
            weaponTrained);
    }

    public static int AttackBonusFromCombatant(
        CFAbilityScoreComponent abilities,
        CFCombatantComponent combatant,
        bool finesse,
        int itemBonus,
        bool weaponTrained)
    {
        var proficiency = CFAttributeMath.ProficiencyBonus(combatant.Level, weaponTrained);
        return AttackModifier(
            abilities.GetModifier(CFAbility.Strength),
            abilities.GetModifier(CFAbility.Dexterity),
            proficiency,
            finesse,
            itemBonus);
    }

    /// <summary>
    /// Attack bonus for a Strike. Ranged always uses Dexterity; melee uses Str (or max Str/Dex if finesse).
    /// </summary>
    public static int AttackBonus(
        CFAbilityScoreComponent abilities,
        CFCombatantComponent combatant,
        CFStrikeWeaponStats weapon,
        bool weaponTrained,
        float distanceFeet = 0f)
    {
        var proficiency = CFAttributeMath.ProficiencyBonus(combatant.Level, weaponTrained);
        int ability;
        if (weapon.Ranged)
            ability = abilities.GetModifier(CFAbility.Dexterity);
        else if (weapon.Finesse)
            ability = Math.Max(abilities.GetModifier(CFAbility.Strength), abilities.GetModifier(CFAbility.Dexterity));
        else
            ability = abilities.GetModifier(CFAbility.Strength);

        var bonus = ability + proficiency + weapon.ItemBonus;
        bonus += RangeIncrementPenalty(distanceFeet, weapon.RangeIncrementFeet);
        bonus += VolleyPenalty(distanceFeet, weapon.VolleyFeet);
        return bonus;
    }

    public static int DamageBonus(
        CFAbilityScoreComponent abilities,
        CFStrikeWeaponStats weapon,
        bool propulsive = false)
    {
        if (weapon.Ranged)
        {
            if (!propulsive)
                return 0;

            var str = abilities.GetModifier(CFAbility.Strength);
            return str < 0 ? str : str / 2;
        }

        return DamageModifier(
            abilities.GetModifier(CFAbility.Strength),
            weapon.Finesse,
            abilities.GetModifier(CFAbility.Dexterity));
    }

    /// <summary>−2 per range increment beyond the first. Attacks beyond 6 increments are impossible (−999).</summary>
    public static int RangeIncrementPenalty(float distanceFeet, int rangeIncrementFeet)
    {
        if (rangeIncrementFeet <= 0 || distanceFeet <= 0f)
            return 0;

        // First increment is 0..range (no penalty). Each further multiple is −2.
        var incrementIndex = (int)Math.Ceiling(distanceFeet / rangeIncrementFeet);
        if (incrementIndex <= 1)
            return 0;

        if (incrementIndex > 6)
            return -999;

        return -2 * (incrementIndex - 1);
    }

    /// <summary>Volley trait: −2 when the target is within the listed distance.</summary>
    public static int VolleyPenalty(float distanceFeet, int volleyFeet)
    {
        if (volleyFeet <= 0)
            return 0;

        return distanceFeet <= volleyFeet ? -2 : 0;
    }

    public static int RollStrikeDamage(
        IRobustRandom random,
        CFStrikeWeaponStats weapon,
        int damageMod,
        bool critical)
    {
        var total = RollDamage(random, weapon.DamageDice, weapon.DamageDie, damageMod, critical);
        if (critical && weapon.DeadlyDie is { } deadly and > 0)
            total += random.Next(1, deadly + 1);

        return Math.Max(0, total);
    }

    private static CFStrikeDegree Improve(CFStrikeDegree degree)
    {
        return degree switch
        {
            CFStrikeDegree.CriticalFailure => CFStrikeDegree.Failure,
            CFStrikeDegree.Failure => CFStrikeDegree.Success,
            CFStrikeDegree.Success => CFStrikeDegree.CriticalSuccess,
            _ => CFStrikeDegree.CriticalSuccess,
        };
    }

    private static CFStrikeDegree Worsen(CFStrikeDegree degree)
    {
        return degree switch
        {
            CFStrikeDegree.CriticalSuccess => CFStrikeDegree.Success,
            CFStrikeDegree.Success => CFStrikeDegree.Failure,
            CFStrikeDegree.Failure => CFStrikeDegree.CriticalFailure,
            _ => CFStrikeDegree.CriticalFailure,
        };
    }
}

public enum CFStrikeDegree : byte
{
    CriticalFailure,
    Failure,
    Success,
    CriticalSuccess,
}
