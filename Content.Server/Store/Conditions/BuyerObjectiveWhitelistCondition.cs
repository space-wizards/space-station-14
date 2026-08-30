using Content.Shared.Mind;
using Content.Shared.Store;
using Content.Shared.Whitelist;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Filters out an entry based on the objectives of an entity.
/// </summary>
public sealed partial class BuyerObjectiveWhitelistCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of objective types.
    /// If there is no whitelist, the object will appear by default.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist of objective types.
    /// If an objective is both whitelisted and blacklisted the blacklist will take priority and the entry will not appear.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var whitelistSystem = ent.System<EntityWhitelistSystem>();

        if (!args.EntityManager.TryGetComponent<MindComponent>(args.Buyer, out var mindComp))
            return true; // inanimate objects don't have minds

        var whitelisted = false;

        foreach (var objective in mindComp.Objectives)
        {
            if (whitelistSystem.IsWhitelistPass(Blacklist, objective))
                return false;
            if (whitelistSystem.IsWhitelistPass(Whitelist, objective))
                whitelisted = true;
        }

        return Whitelist == null || whitelisted;
    }
}
