using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared.Store.Conditions;

/// <summary>
/// Filters out an entry based on the components or tags on an entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BuyerWhitelistCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of tags or components.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist of tags or components.
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var whitelistSystem = ent.System<EntityWhitelistSystem>();

        if (whitelistSystem.IsWhitelistFail(Whitelist, args.Buyer) ||
            whitelistSystem.IsBlacklistPass(Blacklist, args.Buyer))
            return false;

        return true;
    }
}
