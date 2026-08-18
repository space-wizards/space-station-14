using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.RatKing.Components;
using Content.Shared.RatKing.Events;

namespace Content.Shared.RatKing.Systems;

public abstract partial class SharedRatKingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;

    [SubscribeLocalEvent]
    private void OnShutdown(EntityUid uid, RatKingComponent component, ComponentShutdown args)
    {
        foreach (var servant in component.Servants)
        {
            if (TryComp(servant, out RatKingServantComponent? servantComp))
                servantComp.King = null;
        }
    }

    [SubscribeLocalEvent]
    private void OnOrderAction(EntityUid uid, RatKingComponent component, RatKingOrderActionEvent args)
    {
        if (component.CurrentOrder == args.Type)
            return;

        args.Handled = true;

        component.CurrentOrder = args.Type;
        Dirty(uid, component);

        DoCommandCallout(uid, component);
        UpdateActions(uid, component);
        UpdateAllServants(uid, component);
    }

    [SubscribeLocalEvent]
    private void OnServantShutdown(EntityUid uid, RatKingServantComponent component, ComponentShutdown args)
    {
        if (TryComp(component.King, out RatKingComponent? ratKingComponent))
            ratKingComponent.Servants.Remove(uid);
    }

    private void UpdateActions(EntityUid uid, RatKingComponent component)
    {
        if (!TryComp<ActionsComponent>(uid, out var actions))
            return;

        foreach (var action in actions.Actions)
        {
            if (!TryComp<InstantActionComponent>(action, out var instant) ||
                instant.Event is not RatKingOrderActionEvent order)
                continue;

            _action.SetToggled(action, order.Type == component.CurrentOrder);
            _action.StartUseDelay(action);
        }
    }

    public void UpdateAllServants(EntityUid uid, RatKingComponent component)
    {
        foreach (var servant in component.Servants)
        {
            UpdateServantNpc(servant, component.CurrentOrder);
        }
    }

    public virtual void UpdateServantNpc(EntityUid uid, RatKingOrderType orderType) { }

    public virtual void DoCommandCallout(EntityUid uid, RatKingComponent component) { }
}
