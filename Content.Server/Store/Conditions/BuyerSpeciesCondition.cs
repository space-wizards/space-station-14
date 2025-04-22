using Content.Shared.Humanoid;
using Content.Shared.Store;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Content.Shared.Mind;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's species.
/// Supports both blacklists and whitelists.
/// </summary>
public sealed partial class BuyerSpeciesCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of species that can purchase this listing.
    /// </summary>
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpeciesPrototype>))]
    public HashSet<string>? Whitelist;

    /// <summary>
    /// A blacklist of species that cannot purchase this listing.
    /// </summary>
    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpeciesPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;

        if (!ent.TryGetComponent<MindComponent>(args.Buyer, out var mind))
            return true; // needed to obtain body entityuid to check for humanoid appearance

        if (!ent.TryGetComponent<HumanoidAppearanceComponent>(mind.OwnedEntity, out var appearance))
            return true; // inanimate or non-humanoid entities should be handled elsewhere, main example being surplus crates

        if (Blacklist != null)
        {
            if (Blacklist.Contains(appearance.Species))
                return false;
        }

        if (Whitelist != null)
        {
            if (!Whitelist.Contains(appearance.Species))
                return false;
        }

        return true;
    }
}
