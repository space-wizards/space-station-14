namespace Content.Client.Weapons.Melee.Components;

/// <summary>
/// Continuously translates (teleports) the effect to the weapon's user.
/// Used for melee attack animations and muzzle flashes.
/// </summary>
[RegisterComponent]
public sealed partial class WeaponEffectFollowComponent : Component
{
    /// <summary>
    ///  The user to follow; whoever is using the weapon.
    /// </summary>
    public EntityUid User;
}
