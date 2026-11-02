using Robust.Shared.GameStates;

namespace Content.Shared.CF.Alchemy;

/// <summary>
/// Dice heal on activate (PF2e healing potion: minor = 1d8, lesser = 2d8+5, …).
/// Restores CF HP and heals the same total across all SS14 damage types evenly.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFHealConsumableComponent : Component
{
    [DataField, AutoNetworkedField]
    public int HealDice = 1;

    [DataField, AutoNetworkedField]
    public int HealDie = 8;

    [DataField, AutoNetworkedField]
    public int HealBonus;
}
