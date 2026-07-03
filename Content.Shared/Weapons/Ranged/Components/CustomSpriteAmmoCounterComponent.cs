using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// This component replaces the ammo counter UI with one that has custom sprites for the loaded ammo and spent ammo
/// </summary>
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

    /// <summary>
    /// The sprite that will be used in the chamber when the chamber is full
    /// </summary>
    /// <remarks>if null, it will use the <see cref="LoadedAmmoSprite"/> in the chamber</remarks>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? RotatedLoadedAmmoSprite;

    /// <summary>
    /// The sprite that will be used in the chamber when the chamber is empty
    /// </summary>
    /// <remarks>if null, it will use the <see cref="SpentAmmoSprite"/> in the chamber</remarks>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? RotatedSpentAmmoSprite;

    /// <summary>
    /// The number of rows in the ammo counter UI
    /// </summary>
    [DataField, AutoNetworkedField]
    public int NumberOfRows = 2;
}
