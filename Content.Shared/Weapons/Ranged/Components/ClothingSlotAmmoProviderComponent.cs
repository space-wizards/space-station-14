using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// This is used for relaying ammo events
/// to an entity in the user's clothing slot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem))]
public sealed partial class ClothingSlotAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// The slot that the ammo provider should be located in.
    /// </summary>
    [DataField("targetSlot", required: true)]
    public SlotFlags TargetSlot;

    /// <summary>
    /// A whitelist for determining whether or not an ammo provider is valid.
    /// </summary>
    [DataField("providerWhitelist")]
    public EntityWhitelist? ProviderWhitelist;
}
