using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Traitor;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's antag role.
/// Supports both blacklists and whitelists. This is copypaste because roles
/// are absolute shitcode. Refactor this later. -emo
/// </summary>
public sealed class StoreAntagCondition : ListingCondition
{
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AntagPrototype>))]
    public HashSet<string> Whitelist = new();

    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AntagPrototype>))]
    public HashSet<string> Blacklist = new();

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;

        if (!ent.TryGetComponent<MindComponent>(args.User, out var mind) || mind.Mind == null)
            return false;

        var found = false;
        foreach (var role in mind.Mind.AllRoles)
        {
            if (role.GetType() == typeof(TraitorRole))
            {
                var antag = (TraitorRole) role;

                if (Whitelist.Contains(antag.Prototype.ID))
                    found = true;

                if (Blacklist.Contains(antag.Prototype.ID))
                    return false;
            }
        }
        if (!found)
            return false;

        return true;
    }
}
