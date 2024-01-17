namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// allows GunSystem to automatically fire while this component is enabled
/// </summary>

[RegisterComponent]
public sealed partial class AutoShootGunComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;
}
