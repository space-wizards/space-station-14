using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's antag role.
/// Supports both blacklists and whitelists. This is copypaste because roles
/// are absolute shitcode. Refactor this later. -emo
/// </summary>
public sealed partial class BuyerAntagCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of antag roles that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AntagPrototype>))]
    public HashSet<string>? Whitelist;

    /// <summary>
    /// A blacklist of antag roles that cannot purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AntagPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var minds = ent.System<SharedMindSystem>();

        if (!minds.TryGetMind(args.Buyer, out var mindId, out var mind))
            return true;

        var roleSystem = ent.System<SharedRoleSystem>();
        var roles = roleSystem.MindGetAllRoleInfo(mindId);

        if (Blacklist != null)
        {
            foreach (var role in roles)
            {
                if (!role.Antagonist || string.IsNullOrEmpty(role.Prototype))
                    continue;

                if (Blacklist.Contains(role.Prototype))
                    return false;
            }
        }

        if (Whitelist != null)
        {
            var found = false;
            foreach (var role in roles)
            {

                if (!role.Antagonist || string.IsNullOrEmpty(role.Prototype))
                    continue;

                if (Whitelist.Contains(role.Prototype))
                    found = true;
            }
            if (!found)
                return false;
        }

        return true;
    }
}
