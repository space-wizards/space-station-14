using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's job.
/// Supports both blacklists and whitelists
/// </summary>
public sealed partial class BuyerDepartmentCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of department prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<DepartmentPrototype>))]
    public HashSet<string>? Whitelist;

    /// <summary>
    /// A blacklist of department prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<DepartmentPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var ent = args.EntityManager;
        var minds = ent.System<MindSystem>();

        // this is for things like surplus crate
        if (!minds.TryGetMind(args.Buyer, out var mindId, out _))
            return true;

        var jobs = ent.System<JobSystem>();
        if (jobs.MindTryGetJob(mindId, out var job, out _))
        {
            if (Blacklist != null)
            {
                foreach (var department in prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
                {
                    if (job.PrototypeId == null ||
                        !department.Roles.Contains(job.PrototypeId) ||
                        !Blacklist.Contains(department.ID))
                        continue;

                    return false;
                }
            }

            if (Whitelist != null)
            {
                var found = false;
                foreach (var department in prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
                {
                    if (job.PrototypeId == null ||
                        !department.Roles.Contains(job.PrototypeId) ||
                        !Whitelist.Contains(department.ID))
                        continue;

                    found = true;
                    break;
                }

                if (!found)
                    return false;
            }
        }

        return true;
    }
}
