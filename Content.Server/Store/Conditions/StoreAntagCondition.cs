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
public sealed class StoreAntagCondition : ListingCondition
{
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AntagPrototype>))]
    public HashSet<string>? Whitelist;

    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AntagPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;

        if (!ent.TryGetComponent<MindComponent>(args.User, out var mind) || mind.Mind == null)
            return true;

        if (Blacklist != null)
        {
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role.GetType() != typeof(TraitorRole))
                    continue;

                var blacklistantag = (TraitorRole) role;

                if (Blacklist.Contains(blacklistantag.Prototype.ID))
                    return false;
            }
        }

        if (Whitelist != null)
        {
            var found = false;
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role.GetType() == typeof(TraitorRole))
                {
                    var antag = (TraitorRole) role;

                    if (Whitelist.Contains(antag.Prototype.ID))
                        found = true;
                }
            }
            if (!found)
                return false;
        }

        return true;
    }
}
