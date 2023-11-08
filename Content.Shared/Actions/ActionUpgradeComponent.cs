using Robust.Shared.GameStates;

namespace Content.Shared.Actions;

[RegisterComponent, NetworkedComponent, Access(typeof(ActionUpgradeSystem))]
public sealed partial class ActionUpgradeComponent : Component
{
    /// <summary>
    ///     Current Level of the action
    /// </summary>
    [ViewVariables]
    public int Level = 1;

    /// <summary>
    ///     What is the maximum level the action can achieve, if any?
    /// </summary>
    [DataField("maxLevel")]
    public int? MaxLevel;

    // TODO: Increase:
    //  UsesBeforeDelay
    //  Charges
    //    Charges over time?
    //  Damage?

    // TODO: Decrease:
    //  Cooldown/UseDelay
    //  Charges (to null for infinite use)

    // TODO: Exponential?

    // TODO: Name Modifier
    //  Level 1 Fireball => Fireball I
    //  Level 2 Fireball => Fireball II
    //  Level 3 Fireball => Fireball III

}
