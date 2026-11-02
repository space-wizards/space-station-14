using Robust.Shared.GameStates;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder ranged weapon stats. Projectiles shot from this weapon resolve PF2e Strikes on hit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFRangedWeaponComponent : Component
{
    [DataField, AutoNetworkedField]
    public int DamageDie = 8;

    [DataField, AutoNetworkedField]
    public int DamageDice = 1;

    [DataField, AutoNetworkedField]
    public string DamageType = "piercing";

    /// <summary>Proficiency category (e.g. MartialWeapons).</summary>
    [DataField, AutoNetworkedField]
    public string Proficiency = "MartialWeapons";

    [DataField, AutoNetworkedField]
    public int ItemBonus;

    /// <summary>Range increment in feet (longbow = 100).</summary>
    [DataField, AutoNetworkedField]
    public int RangeIncrementFeet = 100;

    /// <summary>
    /// Volley trait distance in feet. Attacks at or inside this range take −2.
    /// 0 disables volley.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int VolleyFeet;

    /// <summary>Deadly trait die faces (longbow deadly d10 → 10). 0 = none.</summary>
    [DataField, AutoNetworkedField]
    public int DeadlyDie;

    /// <summary>
    /// If true, add half positive Strength (or full negative Str) to damage — propulsive trait.
    /// Standard longbow is not propulsive.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Propulsive;

    /// <summary>Agile trait. The second attack is −4 and later attacks are −8, instead of −5/−10.</summary>
    [DataField, AutoNetworkedField]
    public bool Agile;
}
