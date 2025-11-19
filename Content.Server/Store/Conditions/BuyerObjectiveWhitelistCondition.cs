using System.Linq;
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
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist of objective types.
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The default availability of the entry.
    /// </summary>
    [DataField("availableByDefault")]
    public bool AvailableByDefault;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var whitelistSystem = ent.System<EntityWhitelistSystem>();

        if (!args.EntityManager.TryGetComponent<MindComponent>(args.Buyer, out var mindComp))
            return false;

        foreach (var objective in mindComp.Objectives)
        {
            if (whitelistSystem.IsBlacklistPass(Blacklist, objective))
                return false;
            if (whitelistSystem.IsWhitelistPass(Whitelist, objective))
                return true;
        }

        if (!AvailableByDefault)
        {
            return false;
        }
        return true;
    }
}
