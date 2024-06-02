using Content.Shared.Store;
using Content.Shared.Whitelist;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Filters out an entry based on the components or tags on the store itself.
/// </summary>
public sealed partial class StoreWhitelistCondition : ListingCondition
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

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
        if (args.StoreEntity == null)
            return false;

        var ent = args.EntityManager;

        if (_whitelistSystem.IsWhitelistFail(args.StoreEntity.Value, ent) ||
            _whitelistSystem.IsBlacklistPass(args.StoreEntity.Value, ent)
            return false;

        return true;
    }
}
