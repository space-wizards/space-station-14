using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared.Store.Conditions;

/// <summary>
/// Filters out an entry based on the components or tags on the store itself.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class StoreWhitelistCondition : ListingCondition
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
        if (args.StoreEntity == null)
            return false;

        var ent = args.EntityManager;
        var whitelistSystem = ent.System<EntityWhitelistSystem>();

        if (whitelistSystem.IsWhitelistFail(Whitelist, args.StoreEntity.Value) ||
            whitelistSystem.IsBlacklistPass(Blacklist, args.StoreEntity.Value))
            return false;

        return true;
    }
}
