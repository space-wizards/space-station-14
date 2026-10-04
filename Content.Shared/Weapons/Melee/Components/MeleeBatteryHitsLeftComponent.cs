using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// With this component, examining the item will show how many more times it can hit things before the battery is depleted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(MeleeBatteryHitsLeftSystem))]
public sealed partial class MeleeBatteryHitsLeftComponent : Component
{
    /// <summary>
    /// The amount of battery power required to hit with this weapon.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float HitPowerCost = 0f;

    /// <summary>
    /// The text that will be shown on examine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ExamineText = "examine-battery-hits-left";

    /// <summary>
    /// The color of the use count.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.Yellow;
}
