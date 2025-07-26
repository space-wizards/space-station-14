using Content.Shared.Weapons.Ranged;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// This component is used to indicate an entity is shootable from a hitscan weapon.
/// This is placed on the laser entity being shot, not the gun itself.
/// </summary>
[RegisterComponent]
public sealed partial class HitscanAmmoComponent : Component, IShootable;
