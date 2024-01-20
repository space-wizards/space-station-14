using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// allows GunSystem to automatically fire while this component is enabled
/// </summary>

[RegisterComponent, Access(typeof(SharedGunSystem))]
public sealed partial class AutoShootGunComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;
}
