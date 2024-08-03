using Content.Shared.Store;
using Content.Shared.Whitelist;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Filters out an entry based on the components or tags on an entity.
/// </summary>
public sealed partial class BuyerWhitelistCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of tags or components.
    /// </summary>
    [DataField]
    public ItemWhitelist? Whitelist;

    /// <summary>
    /// A blacklist of tags or components.
    /// </summary>
    [DataField]
    public ItemWhitelist? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var whitelistSystem = ent.System<ItemWhitelistSystem>();

        if (whitelistSystem.IsWhitelistFail(Whitelist, args.Buyer) ||
            whitelistSystem.IsBlacklistPass(Blacklist, args.Buyer))
            return false;

        return true;
    }
}
