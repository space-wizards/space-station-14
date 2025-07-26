using Content.Shared.Inventory;

namespace Content.Shared.Whitelist;

public partial class EntityWhitelistSystem
{
    /// <summary>
    /// function to determine if Whitelist is not null and if any entity in an inventory is on the list
    /// </summary>
    public bool IsWhitelistPassInventory(EntityWhitelist? whitelist, EntityUid uid, SlotFlags slots = SlotFlags.WITHOUT_POCKET)
    {
        if (whitelist == null)
            return false;

        var ev = new CheckWhitelistInventoryEvent(whitelist, slots);
        RaiseLocalEvent(uid, ref ev);

        return ev.WhitelistHit;
    }
}

[ByRefEvent]
public record struct CheckWhitelistInventoryEvent(EntityWhitelist List, SlotFlags Slots) : IInventoryRelayEvent
{
    /// <summary>
    /// Whitelist to be checked against
    /// </summary>
    public EntityWhitelist Whitelist => List;

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is on list
    /// </summary>
    public bool WhitelistHit = false;

    SlotFlags IInventoryRelayEvent.TargetSlots => Slots;
}

