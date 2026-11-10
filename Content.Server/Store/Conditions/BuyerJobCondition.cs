using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's job.
/// Supports both blacklists and whitelists
/// </summary>
public sealed partial class BuyerJobCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of jobs prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>>? Whitelist;

    /// <summary>
    /// A blacklist of job prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;

        if (!ent.TryGetComponent<MindComponent>(args.Buyer, out var _))
            return true; // inanimate objects don't have minds

        var jobs = ent.System<SharedJobSystem>();
        jobs.MindTryGetJob(args.Buyer, out var job);

        if (Blacklist != null)
        {
            if (job is not null && Blacklist.Contains(job.ID))
                return false;
        }

        if (Whitelist != null)
        {
            if (job == null || !Whitelist.Contains(job.ID))
                return false;
        }

        return true;
    }
}
