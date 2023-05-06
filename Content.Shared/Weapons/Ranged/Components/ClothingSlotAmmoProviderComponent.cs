using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// This is used for relaying ammo events
/// to an entity in the user's clothing slot.
/// </summary>
[RegisterComponent]
public sealed class ClothingSlotAmmoProviderComponent : AmmoProviderComponent
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
