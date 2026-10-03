using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.RatKing.Components;
using Content.Shared.RatKing.Events;

namespace Content.Shared.RatKing.Systems;

public abstract partial class SharedRatKingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;

    [Dependency] private EntityQuery<InstantActionComponent> _instantActionQuery;
    [Dependency] private EntityQuery<RatKingServantComponent> _servantQuery;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<RatKingComponent> ent, ref ComponentShutdown args)
    {
        foreach (var servant in ent.Comp.Servants)
        {
            if (!_servantQuery.TryComp(servant, out var servantComp))
                continue;

            servantComp.King = null;
            Dirty(servant, servantComp);
        }
    }

    [SubscribeLocalEvent]
    private void OnOrderAction(Entity<RatKingComponent> ent, ref RatKingOrderActionEvent args)
    {
        if (ent.Comp.CurrentOrder == args.Type)
            return;

        args.Handled = true;

        ent.Comp.CurrentOrder = args.Type;
        Dirty(ent);

        DoCommandCallout(ent);
        UpdateOrderActions(ent);
        UpdateAllServants(ent);
    }

    [SubscribeLocalEvent]
    private void OnServantShutdown(Entity<RatKingServantComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.King is not { } king ||
            !TryComp<RatKingComponent>(king, out var ratKing))
            return;

        ratKing.Servants.Remove(ent.Owner);
    }

    private void UpdateOrderActions(Entity<RatKingComponent> ent)
    {
        if (!TryComp<ActionsComponent>(ent.Owner, out var actions))
            return;

        foreach (var action in actions.Actions)
        {
            if (!_instantActionQuery.TryComp(action, out var instant) ||
                instant.Event is not RatKingOrderActionEvent order)
                continue;

            _action.SetToggled(action, order.Type == ent.Comp.CurrentOrder);
            _action.StartUseDelay(action);
        }
    }

    private void UpdateAllServants(Entity<RatKingComponent> ent)
    {
        foreach (var servant in ent.Comp.Servants)
        {
            UpdateServantNpc(servant, ent.Comp.CurrentOrder);
        }
    }

    protected virtual void UpdateServantNpc(EntityUid uid, RatKingOrderType orderType) { }

    protected virtual void DoCommandCallout(Entity<RatKingComponent> ent) { }
}
