using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions.Events;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

public sealed class ActionUpgradeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionUpgradeComponent, ActionUpgradeIncreaseEvent>(OnActionUpgradeIncreaseEvent);
        SubscribeLocalEvent<ActionUpgradeComponent, ActionUpgradeDecreaseEvent>(OnActionUpgradeDecreaseEvent);
    }

    // TODO: Method that calls level up, which fires off the level up event
    // When level event is called, make sure other events can fire
    public void UpgradeAction(EntityUid? actionId, ActionUpgradeComponent? actionUpgradeComponent = null)
    {
        if (actionUpgradeComponent == null)
        {
            if (!TryGetActionUpgrade(actionId, out var actionUpgradeComp))
                return;

            actionUpgradeComponent = actionUpgradeComp;
        }

        /*var ev = new ActionUpgradeEvent();
        RaiseLocalEvent(uid.Value, ref ev);
        result = ev.Action;*/
    }

    public bool TryGetActionUpgrade(
        [NotNullWhen(true)] EntityUid? uid,
        [NotNullWhen(true)] out ActionUpgradeComponent? result,
        bool logError = true)
    {
        result = null;
        if (!Exists(uid))
            return false;

        if (!TryComp<ActionUpgradeComponent>(uid, out var actionUpgradeComponent))
        {
            Log.Error($"Failed to get action upgrade from action entity: {ToPrettyString(uid.Value)}");
            return false;
        }

        result = actionUpgradeComponent;
        DebugTools.AssertOwner(uid, result);
        return true;
    }

    private void OnActionUpgradeIncreaseEvent(EntityUid uid, ActionUpgradeComponent component, ActionUpgradeIncreaseEvent args)
    {
        if (!_actions.TryGetActionData(uid, out var action))
            return;

        if (action.Charges != null && component.ChargeChangeAmount is > 0)
            _actions.AddCharges(uid, component.ChargeChangeAmount.Value);

        if (component.UsesBeforeDelayChangeAmount > 0)
            _actions.AddUsesBeforeDelay(uid, component.UsesBeforeDelayChangeAmount);
    }

    private void OnActionUpgradeDecreaseEvent(EntityUid uid, ActionUpgradeComponent component, ActionUpgradeDecreaseEvent args)
    {
        if (!_actions.TryGetActionData(uid, out var action))
            return;

        if (action.Charges != null)
            _actions.RemoveCharges(uid, component.ChargeChangeAmount);

        if (action.UseDelay != null)
            _actions.ReduceUseDelay(uid, component.DelayChangeAmount);
    }

    //   TODO: Event change?

    // TODO: ALT IDEA - Just change prototypes instead?
    //   Which one would be faster?
}
