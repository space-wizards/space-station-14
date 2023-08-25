using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Prototypes;

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

        if (!ent.TryGetComponent<MindContainerComponent>(args.Buyer, out var mind) || mind.Mind == null)
            return true; //this is for things like surplus crate

        if (Blacklist != null)
        {
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role is not Job job)
                    continue;

                foreach (var department in prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
                    if (department.Roles.Contains(job.Prototype.ID))
                        if (Blacklist.Contains(department.ID))
                            return false;
            }
        }

        if (Whitelist != null)
        {
            var found = false;
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role is not Job job)
                    continue;

                foreach (var department in prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
                    if (department.Roles.Contains(job.Prototype.ID))
                        if (Whitelist.Contains(department.ID))
                        {
                            found = true;
                            break;
                        }

                if (found)
                    break;
            }
            if (!found)
                return false;
        }

        return true;
    }
}
