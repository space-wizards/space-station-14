using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a gun when it would like to take the specified amount of ammo.
/// </summary>
public sealed class TakeAmmoEvent : EntityEventArgs
{
    public EntityUid? User;
    public readonly int Shots;
    public List<IShootable> Ammo;

    /// <summary>
    /// Coordinates to spawn the ammo at.
    /// </summary>
    public EntityCoordinates Coordinates;

    public TakeAmmoEvent(int shots, List<IShootable> ammo, EntityCoordinates coordinates, EntityUid? user)
    {
        Shots = shots;
        Ammo = ammo;
        Coordinates = coordinates;
        User = user;
    }
}
