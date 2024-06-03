using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows GunSystem to automatically fire while this component is enabled
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem)), AutoGenerateComponentState]
public sealed partial class AutoShootGunComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled;
}
