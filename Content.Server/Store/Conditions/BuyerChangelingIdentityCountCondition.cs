using System.Linq;
using Content.Shared.Changeling.Components;
using Content.Shared.Mind;
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Only allows this listing being purchased when the buyer is a changeling that has devoured X (Count) unique bodies.
/// </summary>
public sealed partial class BuyerChangelingIdentityCountCondition : ListingCondition
{
    /// <summary>
    /// How many bodies need to have been devoured for this listing to become available.
    /// </summary>
    [DataField]
    public int Count;

    /// <summary>
    /// Whether the identities need to be devoured.
    /// </summary>
    [DataField]
    public bool RequireDevour;

    public override bool Condition(ListingConditionArgs args)
    {
        if (!args.EntityManager.TryGetComponent<MindComponent>(args.Buyer, out var mind))
            return false; // needs a body to be a changeling...

        if (!args.EntityManager.TryGetComponent<ChangelingIdentityComponent>(mind.OwnedEntity, out var comp))
            return false; // not a changeling

        var count = comp.ConsumedIdentities.Count((e) => (!RequireDevour || e.GrantedDna) && !e.Starting);
        return count >= Count;
    }
}
