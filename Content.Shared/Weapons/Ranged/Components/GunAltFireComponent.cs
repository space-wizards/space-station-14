using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Enables a gun to use an "alt fire" that shoots the gun in a different mode with a secondary firing button.
/// Requires <see cref="GunComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunAltFireComponent : Component
{
    /// <summary>
    /// The fire mode alt fire should utilize.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SelectiveFire AltFireMode = SelectiveFire.Burst;

    /// <summary>
    /// If true, the gun will force the user into wielding when firing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ForceWielding = true;
}
