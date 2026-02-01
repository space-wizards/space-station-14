using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Whitelist;

public sealed partial class EntityWhitelistSystem
{
    /// <summary>
    /// Checks whether a given EntProtoId satisfies a whitelist.
    /// </summary>
    public bool IsValid(EntityWhitelist list, [ForbidLiteral] EntProtoId protoId)
    {
        return IsValid(list, _proto.Index(protoId));
    }

    /// <summary>
    /// Checks whether a given EntityPrototype satisfies a whitelist.
    /// </summary>
    public bool IsValid(EntityWhitelist list, EntityPrototype prototype)
    {
        list.Registrations ??= StringsToRegs(list.Components);

        if (list.Registrations != null)
        {
            foreach (var reg in list.Registrations)
            {
                if (prototype.Components.ContainsKey(reg.Name))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        if (list.Sizes != null && prototype.TryGetComponent(_itemComponentName, out ItemComponent? itemComp))
        {
            if (list.Sizes.Contains(itemComp.Size))
                return true;
        }

        if (list.Tags != null && prototype.TryGetComponent(_tagComponentName, out TagComponent? tagComp))
        {
            return list.RequireAll
                ? _tag.HasAllTags(tagComp, list.Tags)
                : _tag.HasAnyTag(tagComp, list.Tags);
        }

        return list.RequireAll;
    }

    /// <summary>
    /// Checks whether a given EntProtoId is allowed by a whitelist and not blocked by a blacklist.
    /// If a blacklist is provided and it matches then this returns false.
    /// If a whitelist is provided and it does not match then this returns false.
    /// If either list is null it does not get checked.
    /// </summary>
    public bool CheckBoth([ForbidLiteral] EntProtoId protoId, EntityWhitelist? blacklist = null, EntityWhitelist? whitelist = null)
    {
        return CheckBoth(_proto.Index(protoId), blacklist, whitelist);
    }

    /// <summary>
    /// Checks whether a given EntityPrototype is allowed by a whitelist and not blocked by a blacklist.
    /// If a blacklist is provided and it matches then this returns false.
    /// If a whitelist is provided and it does not match then this returns false.
    /// If either list is null it does not get checked.
    /// </summary>
    public bool CheckBoth(EntityPrototype prototype, EntityWhitelist? blacklist = null, EntityWhitelist? whitelist = null)
    {
        if (blacklist != null && IsValid(blacklist, prototype))
            return false;

        return whitelist == null || IsValid(whitelist, prototype);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is not null and the EntProtoId is on the list.
    /// </summary>
    public bool IsWhitelistPass(EntityWhitelist? whitelist, [ForbidLiteral] EntProtoId protoId)
    {
        if (whitelist == null)
            return false;

        return IsValid(whitelist, protoId);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is not null and the EntityPrototype is on the list.
    /// </summary>
    public bool IsWhitelistPass(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        if (whitelist == null)
            return false;

        return IsValid(whitelist, prototype);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is not null and the EntProtoId is not on the list.
    /// </summary>
    public bool IsWhitelistFail(EntityWhitelist? whitelist, [ForbidLiteral] EntProtoId protoId)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, protoId);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is not null and the EntityPrototype is not on the list.
    /// </summary>
    public bool IsWhitelistFail(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, prototype);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is either null or the EntProtoId is on the list.
    /// </summary>
    public bool IsWhitelistPassOrNull(EntityWhitelist? whitelist, [ForbidLiteral] EntProtoId protoId)
    {
        if (whitelist == null)
            return true;

        return IsValid(whitelist, protoId);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is either null or the EntityPrototype is on the list.
    /// </summary>
    public bool IsWhitelistPassOrNull(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        if (whitelist == null)
            return true;

        return IsValid(whitelist, prototype);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is either null or the EntProtoId is not on the list.
    /// </summary>
    public bool IsWhitelistFailOrNull(EntityWhitelist? whitelist, [ForbidLiteral] EntProtoId protoId)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, protoId);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is either null or the EntityPrototype is not on the list.
    /// </summary>
    public bool IsWhitelistFailOrNull(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, prototype);
    }
}
