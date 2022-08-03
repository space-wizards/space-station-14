using Content.Shared.Store;
using Content.Shared.Whitelist;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Filters out an entry based on the components or tags on an entity.
/// </summary>
public sealed class StoreUserWhitelistCondition : ListingCondition
{
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;

        if (Whitelist != null)
        {
            if (!Whitelist.IsValid(args.User, ent))
                return false;
        }

        if (Blacklist != null)
        {
            if (Blacklist.IsValid(args.User, ent))
                return false;
        }

        return true;
    }
}
