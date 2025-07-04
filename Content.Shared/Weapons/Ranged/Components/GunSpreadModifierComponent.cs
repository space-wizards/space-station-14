using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// This component modifies the spread of the gun it is attached to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunSpreadModifierComponent: Component
{
    /// <summary>
    /// A scalar value multiplied by the spread built into the ammo itself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Spread = 1;
}
