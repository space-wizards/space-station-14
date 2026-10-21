using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// This is used for relaying ammo events
/// to an entity in the user's clothing slot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem))]
public sealed partial class ClothingSlotAmmoProviderComponent : AmmoProviderComponent;
