using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

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
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<JobPrototype>))]
    public HashSet<string>? Whitelist;

    /// <summary>
    /// A blacklist of job prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<JobPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var minds = ent.System<SharedMindSystem>();

        // this is for things like surplus crate
        if (!minds.TryGetMind(args.Buyer, out var mindId, out _))
            return true;

        var jobs = ent.System<SharedJobSystem>();
        jobs.MindTryGetJob(mindId, out var job);

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
