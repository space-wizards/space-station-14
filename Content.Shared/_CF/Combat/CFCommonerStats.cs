using Content.Shared.CF.Preferences;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder 2e Commoner (Creature -1) — used as the default for vanilla SS14 mobs.
/// Source: NPC Core / https://2e.aonprd.com/NPCs.aspx?ID=3488
/// </summary>
public static class CFCommonerStats
{
    public const int Level = -1;
    public const int ArmorClass = 13;
    public const int HitPoints = 10;
    public const int Fortitude = 6;
    public const int Reflex = 3;
    public const int Will = 3;

    public static readonly Dictionary<CFAbility, int> Modifiers = new()
    {
        { CFAbility.Strength, 3 },
        { CFAbility.Dexterity, 1 },
        { CFAbility.Constitution, 2 },
        { CFAbility.Intelligence, 0 },
        { CFAbility.Wisdom, 1 },
        { CFAbility.Charisma, 0 },
    };

    /// <summary>Stationary / unattended objects are easy to hit.</summary>
    public const int ObjectArmorClass = 10;
}
