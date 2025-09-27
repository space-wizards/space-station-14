namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised to get the active weapon entity for a given user.
/// </summary>
[ByRefEvent]
public struct GetActiveWeaponEvent
{
    public EntityUid? Weapon;

    public bool Handled;
}
