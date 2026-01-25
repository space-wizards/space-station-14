using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Enables a gun to use an "alt fire" that shoots the gun in burst fire mode.
/// Requires <see cref="GunComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunAltFireComponent : Component
{
    /// <summary>
    /// If true, the gun will force the user into wielding when firing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SelectiveFire AltFireType = SelectiveFire.Burst;

    /// <summary>
    /// If true, the gun will force the user into wielding when firing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ForceWielding = true;
}
