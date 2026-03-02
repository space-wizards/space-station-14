using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised before a entity is loaded into a gun (or a magazine) as ammo
/// </summary>
[ByRefEvent]
public struct GetAmmoEvent
{
    public bool CanLoad => true;
    public EntityUid? AmmoOverride;
}
