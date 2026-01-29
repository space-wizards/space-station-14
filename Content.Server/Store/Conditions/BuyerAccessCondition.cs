using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's ID's access.
/// Supports both blacklists and whitelists
/// </summary>
public sealed partial class BuyerAccessCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of access prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
    public HashSet<string>? Whitelist;

    /// <summary>
    /// A blacklist of access prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var accessReader = ent.System<AccessReaderSystem>();

        var buyerEntity = args.Buyer;
        if (ent.TryGetComponent<MindComponent>(args.Buyer, out var mind) && mind.CurrentEntity != null)
        {
            buyerEntity = mind.CurrentEntity.Value;
        }

        var accessTags = accessReader.FindAccessTags(buyerEntity);

        if (Blacklist != null && accessTags.Any(tag => Blacklist.Contains(tag)))
        {
            return false;
        }

        if (Whitelist != null && !accessTags.Any(tag => Whitelist.Contains(tag)))
        {
            return false;
        }

        return true;
    }
}
