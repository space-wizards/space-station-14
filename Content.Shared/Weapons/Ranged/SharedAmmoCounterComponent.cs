using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged;

/// <summary>
/// Shows an ItemStatus with the ammo of the gun. Adjusts based on what the ammoprovider is.
/// </summary>
[NetworkedComponent]
public abstract class SharedAmmoCounterComponent : Component {}
