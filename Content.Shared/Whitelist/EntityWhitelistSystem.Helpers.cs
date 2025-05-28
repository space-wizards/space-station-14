using Content.Shared.Inventory;

namespace Content.Shared.Whitelist;

public partial class EntityWhitelistSystem
{
    /// The following are a list of "helper functions" that are basically the same as each other
    /// to help make code that uses EntityWhitelist a bit more readable because at the moment
    /// it is quite clunky having to write out component.Whitelist == null ? true : _whitelist.IsValid(component.Whitelist, uid)
    /// several times in a row and makes comparisons easier to read

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is on list
    /// </summary>
    public bool IsWhitelistPass(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is not on the list
    /// </summary>
    public bool IsWhitelistFail(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is on the list
    /// </summary>
    public bool IsWhitelistPassOrNull(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is not on the list
    /// </summary>
    public bool IsWhitelistFailOrNull(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is not null and entity is on list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistPass(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPass(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is not null and entity is not on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistFail(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFail(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is either null or the entity is on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistPassOrNull(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPassOrNull(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is either null or the entity is not on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistFailOrNull(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFailOrNull(blacklist, uid);
    }

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

