using System.Diagnostics;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.Store.Conditions;

public sealed partial class BuyerSpeciesCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of species prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpeciesPrototype>))]
    public HashSet<string>? Whitelist;

    /// <summary>
    /// A blacklist of species prototypes that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpeciesPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        if (!ent.TryGetComponent<HumanoidAppearanceComponent>(args.Buyer, out var appearance))
            return true; //return true for the surplus crate; replicates department condition

        if (Blacklist != null)
        {
            if (Blacklist.Contains(appearance.Species.Id))
            {
                return false;
            }
        }

        if (Whitelist != null)
        {
            if (Whitelist.Contains(appearance.Species.Id))
            {
                return true;
            }
        }

        return false; //return false if there is somehow no whitelist or blacklist
    }
}
