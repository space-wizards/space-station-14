using Robust.Shared.GameStates;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder melee weapon stats used for Strikes instead of stock SS14 damage.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFMeleeWeaponComponent : Component
{
    /// <summary>Damage die faces (e.g. 8 for a d8 longsword).</summary>
    [DataField, AutoNetworkedField]
    public int DamageDie = 8;

    /// <summary>Number of damage dice (usually 1).</summary>
    [DataField, AutoNetworkedField]
    public int DamageDice = 1;

    [DataField, AutoNetworkedField]
    public string DamageType = "slashing";

    /// <summary>Weapon proficiency category matching class Attacks lists (e.g. MartialWeapons).</summary>
    [DataField, AutoNetworkedField]
    public string Proficiency = "MartialWeapons";

    /// <summary>If true, attack/damage can use Dexterity when higher than Strength.</summary>
    [DataField, AutoNetworkedField]
    public bool Finesse;

    /// <summary>Agile trait. The second attack is −4 and later attacks are −8, instead of −5/−10.</summary>
    [DataField, AutoNetworkedField]
    public bool Agile;

    [DataField, AutoNetworkedField]
    public int ItemBonus;

    /// <summary>
    /// Pathfinder reach in feet. 5 threatens adjacent tiles. 10 is the reach trait
    /// and also lengthens the SS14 swing so the weapon can Strike at that distance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ReachFeet = CFFlanking.DefaultReachFeet;
}
