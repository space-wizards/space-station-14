using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Abilities.MinionMaster;

public abstract class SharedMinionMasterSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MinionMasterComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MinionMasterComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MinionMasterComponent, MinionMasterOrderActionEvent>(OnOrderAction);

        SubscribeLocalEvent<MinionComponent, ComponentShutdown>(OnServantShutdown);
    }

    private void OnStartup(EntityUid uid, MinionMasterComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.AddAction(uid, ref component.ActionRaiseArmyEntity, component.ActionRaiseArmy, component: comp);
        _action.AddAction(uid, ref component.ActionOrderStayEntity, component.ActionOrderStay, component: comp);
        _action.AddAction(uid, ref component.ActionOrderFollowEntity, component.ActionOrderFollow, component: comp);
        _action.AddAction(uid, ref component.ActionOrderAttackEntity, component.ActionOrderCheeseEm, component: comp);
        _action.AddAction(uid, ref component.ActionOrderLooseEntity, component.ActionOrderLoose, component: comp);

        UpdateActions(uid, component);
    }

    private void OnShutdown(EntityUid uid, MinionMasterComponent component, ComponentShutdown args)
    {
        foreach (var minion in component.Minions)
        {
            if (TryComp(minion, out MinionComponent? minionComp))
                minionComp.Master = null;
        }

        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.RemoveAction(uid, component.ActionRaiseArmyEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderStayEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderFollowEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderAttackEntity, comp);
        _action.RemoveAction(uid, component.ActionOrderLooseEntity, comp);
    }

    private void OnOrderAction(EntityUid uid, MinionMasterComponent component, MinionMasterOrderActionEvent args)
    {
        if (component.CurrentOrder == args.Type)
            return;
        args.Handled = true;

        component.CurrentOrder = args.Type;
        Dirty(uid, component);

        DoCommandCallout(uid, component);
        UpdateActions(uid, component);
        UpdateAllMinions(uid, component);
    }

    private void OnServantShutdown(EntityUid uid, MinionComponent component, ComponentShutdown args)
    {
        if (TryComp(component.Master, out MinionMasterComponent? minionMasterComponent))
            minionMasterComponent.Minions.Remove(uid);
    }

    private void UpdateActions(EntityUid uid, MinionMasterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _action.SetToggled(component.ActionOrderStayEntity, component.CurrentOrder == MinionOrderType.Stay);
        _action.SetToggled(component.ActionOrderFollowEntity, component.CurrentOrder == MinionOrderType.Follow);
        _action.SetToggled(component.ActionOrderAttackEntity, component.CurrentOrder == MinionOrderType.Attack);
        _action.SetToggled(component.ActionOrderLooseEntity, component.CurrentOrder == MinionOrderType.Loose);
        _action.StartUseDelay(component.ActionOrderStayEntity);
        _action.StartUseDelay(component.ActionOrderFollowEntity);
        _action.StartUseDelay(component.ActionOrderAttackEntity);
        _action.StartUseDelay(component.ActionOrderLooseEntity);
    }

    public void UpdateAllMinions(EntityUid uid, MinionMasterComponent component)
    {
        foreach (var minion in component.Minions)
        {
            UpdateMinionNpc(minion, component.CurrentOrder);
        }
    }

    public virtual void UpdateMinionNpc(EntityUid uid, MinionOrderType orderType)
    {

    }

    public virtual void DoCommandCallout(EntityUid uid, MinionMasterComponent component)
    {

    }
}
