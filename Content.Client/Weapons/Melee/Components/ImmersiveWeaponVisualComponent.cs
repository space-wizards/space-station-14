using Robust.Shared.Utility;

namespace Content.Client.Weapons.Melee.Components;

/// <summary>
/// an alternative system that allows you to display the sprite of an weapon that is used in combat
/// </summary>
[RegisterComponent]
public sealed partial class ImmersiveWeaponVisualsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier WeaponSprite;
}
