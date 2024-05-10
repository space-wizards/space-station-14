using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Only allows a listing to be purchased a certain amount of times.
/// </summary>
public sealed partial class ListingLimitedUpgradesCondition : ListingCondition
{
    /// <summary>
    /// The amount of times this listing can be purchased.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<ActionUpgradeComponent> Prototype;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var minds = ent.System<SharedMindSystem>();

        if (!minds.TryGetMind(args.Buyer, out var mindId, out _))
            return false;

        if (!args.EntityManager.TryGetComponent(mindId, out ActionsContainerComponent? actionsContainer))
            return false;

        EntityUid targetAction = default;
        foreach (var action in actionsContainer.Container.ContainedEntities)
        {
            var actionPrototype = args.EntityManager.GetComponent<MetaDataComponent>(action).EntityPrototype;
            if (actionPrototype == null || actionPrototype.ID != Prototype)
                continue;

            targetAction = action;
        }

        if (!args.EntityManager.TryGetComponent(targetAction, out ActionUpgradeComponent? actionUpgrade))
            return false;

        return actionUpgrade.EffectedLevels.Count >= actionUpgrade.Level-1;
    }
}
