using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CustomSpriteAmmoCounterComponent : Component
{
    /// <summary>
    /// The sprite that will be used to count the ammo
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier LoadedAmmoSprite;

    /// <summary>
    /// The sprite that will be used to count the ammo when the ammo is spent
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier SpentAmmoSprite;
}
