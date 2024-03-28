using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This is used for an entity with <see cref="MeleeThrowOnHitComponent"/> that is governed by an gas tank inside of it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GasPoweredThrowerComponent : Component
{
    /// <summary>
    /// The ID of the item slot containing the gas tank.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string TankSlotId = "gas_tank";

    /// <summary>
    /// Amount of moles to consume for each melee attack.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float GasUsage = 0.142f;
}
