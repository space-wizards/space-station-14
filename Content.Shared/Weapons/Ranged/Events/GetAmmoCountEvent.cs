namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on an AmmoProvider to request deets.
/// </summary>
[ByRefEvent]
public struct GetAmmoCountEvent
{
    public int Count;
    public int Capacity;
}