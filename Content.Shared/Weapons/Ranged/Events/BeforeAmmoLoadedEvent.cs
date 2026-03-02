using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on the ammo before it is loaded into a gun (or a magazine)
/// </summary>
[ByRefEvent]
public struct BeforeAmmoLoadedEvent()
{
    /// <summary>
    /// if the entity can be used to load the gun or magazine
    /// </summary>
    public bool CanLoad = true;

    /// <summary>
    /// If null the entity itself is used to load a gun or magazine,
    /// if not null, the entity provided is used to load the gun or magazine
    /// </summary>
    public EntityUid? AmmoOverride;
}
