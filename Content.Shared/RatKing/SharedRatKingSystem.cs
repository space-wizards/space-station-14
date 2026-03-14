using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.RatKing;

public abstract class SharedRatKingSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RatKingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RatKingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RatKingComponent, RatKingOrderActionEvent>(OnOrderAction);
        SubscribeLocalEvent<RatKingServantComponent, ComponentShutdown>(OnServantShutdown);
    }

    private void OnStartup(EntityUid uid, RatKingComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.AddAction(uid, ref component.ActionRaiseArmyEntity, component.ActionRaiseArmy, component: comp);
        _action.AddAction(uid, ref component.ActionDomainEntity, component.ActionDomain, component: comp);
        _action.AddAction(uid, ref component.ActionOrderStayEntity, component.ActionOrderStay, component: comp);
        _action.AddAction(uid, ref component.ActionOrderFollowEntity, component.ActionOrderFollow, component: comp);
        _action.AddAction(uid, ref component.ActionOrderCheeseEmEntity, component.ActionOrderCheeseEm, component: comp);
        _action.AddAction(uid, ref component.ActionOrderLooseEntity, component.ActionOrderLoose, component: comp);

        UpdateActions(uid, component);
    }

    private void OnShutdown(EntityUid uid, RatKingComponent component, ComponentShutdown args)
    {
        foreach (var servant in component.Servants)
        {
            if (TryComp(servant, out RatKingServantComponent? servantComp))
                servantComp.King = null;
        }

        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        var actions = new Entity<ActionsComponent?>(uid, comp);
        _action.RemoveAction(actions, component.ActionRaiseArmyEntity);
        _action.RemoveAction(actions, component.ActionDomainEntity);
        _action.RemoveAction(actions, component.ActionOrderStayEntity);
        _action.RemoveAction(actions, component.ActionOrderFollowEntity);
        _action.RemoveAction(actions, component.ActionOrderCheeseEmEntity);
        _action.RemoveAction(actions, component.ActionOrderLooseEntity);
    }

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

    private void OnServantShutdown(EntityUid uid, RatKingServantComponent component, ComponentShutdown args)
    {
        if (TryComp(component.King, out RatKingComponent? ratKingComponent))
            ratKingComponent.Servants.Remove(uid);
    }

    private void UpdateActions(EntityUid uid, RatKingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _action.SetToggled(component.ActionOrderStayEntity, component.CurrentOrder == RatKingOrderType.Stay);
        _action.SetToggled(component.ActionOrderFollowEntity, component.CurrentOrder == RatKingOrderType.Follow);
        _action.SetToggled(component.ActionOrderCheeseEmEntity, component.CurrentOrder == RatKingOrderType.CheeseEm);
        _action.SetToggled(component.ActionOrderLooseEntity, component.CurrentOrder == RatKingOrderType.Loose);
        _action.StartUseDelay(component.ActionOrderStayEntity);
        _action.StartUseDelay(component.ActionOrderFollowEntity);
        _action.StartUseDelay(component.ActionOrderCheeseEmEntity);
        _action.StartUseDelay(component.ActionOrderLooseEntity);
    }

    public void UpdateAllServants(EntityUid uid, RatKingComponent component)
    {
        foreach (var servant in component.Servants)
        {
            UpdateServantNpc(servant, component.CurrentOrder);
        }
    }

    public virtual void UpdateServantNpc(EntityUid uid, RatKingOrderType orderType)
    {

    }

    public virtual void DoCommandCallout(EntityUid uid, RatKingComponent component)
    {

    }
}
