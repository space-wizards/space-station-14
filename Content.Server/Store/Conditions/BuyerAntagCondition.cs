using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's antag role.
/// Supports both blacklists and whitelists. This is copypaste because roles
/// are absolute shitcode. Refactor this later. -emo
/// </summary>
public sealed class BuyerAntagCondition : ListingCondition
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

        if (!ent.TryGetComponent<MindComponent>(args.Buyer, out var mind) || mind.Mind == null)
            return true;

        if (Blacklist != null)
        {
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role is not TraitorRole blacklistantag)
                    continue;

                if (Blacklist.Contains(blacklistantag.Prototype.ID))
                    return false;
            }
        }

        if (Whitelist != null)
        {
            var found = false;
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role is not TraitorRole antag)
                    continue;

                if (Whitelist.Contains(antag.Prototype.ID))
                    found = true;
            }
            if (!found)
                return false;
        }

        return true;
    }
}
