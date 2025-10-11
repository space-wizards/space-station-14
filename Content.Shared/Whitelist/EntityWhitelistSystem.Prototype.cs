using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Whitelist;

public sealed partial class EntityWhitelistSystem
{
    /// <summary>
    /// Checks whether a given entity prototype satisfies a whitelist.
    /// </summary>
    public bool IsValid(EntityWhitelist list, [ForbidLiteral] EntProtoId protoId)
    {
        var prototype = _proto.Index(protoId);

        if (list.Components != null)
        {
            foreach (var comp in list.Components)
            {
                if (prototype.Components.ContainsKey(comp))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        if (list.Sizes != null && prototype.TryGetComponent<ItemComponent>(out var itemComp, Factory))
        {
            if (list.Sizes.Contains(itemComp.Size))
                return true;
        }

        if (list.Tags != null && prototype.TryGetComponent<TagComponent>(out var tagComp, Factory))
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
        if (blacklist != null && IsValid(blacklist, protoId))
            return false;

        return whitelist == null || IsValid(whitelist, protoId);
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
    /// Helper function to determine if a whitelist is not null and the EntProtoId is not on the list.
    /// </summary>
    public bool IsWhitelistFail(EntityWhitelist? whitelist, [ForbidLiteral] EntProtoId protoId)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, protoId);
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
    /// Helper function to determine if a whitelist is either null or the EntProtoId is not on the list.
    /// </summary>
    public bool IsWhitelistFailOrNull(EntityWhitelist? whitelist, [ForbidLiteral] EntProtoId protoId)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, protoId);
    }

    /// <summary>
    /// Helper function to determine if a blacklist is not null and the EntProtoId is on the list.
    /// Duplicate of equivalent whitelist function.
    /// </summary>
    public bool IsBlacklistPass(EntityWhitelist? blacklist, [ForbidLiteral] EntProtoId protoId)
    {
        return IsWhitelistPass(blacklist, protoId);
    }

    /// <summary>
    /// Helper function to determine if a blacklist is not null and EntProtoId is not on the list.
    /// Duplicate of equivalent whitelist function.
    /// </summary>
    public bool IsBlacklistFail(EntityWhitelist? blacklist, [ForbidLiteral] EntProtoId protoId)
    {
        return IsWhitelistFail(blacklist, protoId);
    }

    /// <summary>
    /// Helper function to determine if a blacklist is either null or the EntProtoId is on the list.
    /// Duplicate of equivalent whitelist function.
    /// </summary>
    public bool IsBlacklistPassOrNull(EntityWhitelist? blacklist, [ForbidLiteral] EntProtoId protoId)
    {
        return IsWhitelistPassOrNull(blacklist, protoId);
    }

    /// <summary>
    /// Helper function to determine if a blacklist is either null or the EntProtoId is not on the list.
    /// Duplicate of equivalent whitelist function.
    /// </summary>
    public bool IsBlacklistFailOrNull(EntityWhitelist? blacklist, [ForbidLiteral] EntProtoId protoId)
    {
        return IsWhitelistFailOrNull(blacklist, protoId);
    }
}
