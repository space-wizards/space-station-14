using Robust.Shared.GameStates;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder armor item bonus / Dex cap applied to Armor Class when worn.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFArmorComponent : Component
{
    /// <summary>Item bonus to AC (e.g. +1 leather, +2 hide).</summary>
    [DataField, AutoNetworkedField]
    public int ItemBonus = 1;

    /// <summary>Maximum Dexterity modifier that applies while wearing this armor. Null = no cap.</summary>
    [DataField, AutoNetworkedField]
    public int? DexCap = 3;

    /// <summary>Proficiency category matching class Defenses lists (LightArmor, MediumArmor, …).</summary>
    [DataField, AutoNetworkedField]
    public string Proficiency = "LightArmor";
}
