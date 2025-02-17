// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DeadSpace.Necromorphs.Unitology;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Server.Popups;
using Content.Shared.Stunnable;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Server.DeadSpace.Necromorphs.Unitology.Abilities;
using Content.Shared.StatusEffect;
using Content.Shared.Speech.Muting;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class UnitologyHeadSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyHeadComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<UnitologyHeadComponent, ComponentShutdown>(OnShutDown);
        SubscribeLocalEvent<UnitologyHeadComponent, UnitologyHeadActionEvent>(OnHeadUnitology);
        SubscribeLocalEvent<UnitologyHeadComponent, OrderToSlaveActionEvent>(OnOrder);
    }

    private void OnComponentInit(EntityUid uid, UnitologyHeadComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionUnitologyHeadEntity, component.ActionUnitologyHead, uid);
        _actionsSystem.AddAction(uid, ref component.ActionOrderToSlaveEntity, component.ActionOrderToSlave, uid);
    }

    private void OnShutDown(EntityUid uid, UnitologyHeadComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionUnitologyHeadEntity);
        _actionsSystem.RemoveAction(uid, component.ActionOrderToSlaveEntity);
    }

    private void OnOrder(EntityUid uid, UnitologyHeadComponent component, OrderToSlaveActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        var target = args.Target;

        if (!HasComp<UnitologyEnslavedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель должна быть подчинена!"), uid, uid);
            return;
        }

        if (!HasComp<StunSlaveComponent>(target))
        {
            AddComp<StunSlaveComponent>(target);
            _popup.PopupEntity(Loc.GetString("Цель парализованна."), uid, uid);
        }
        else
        {
            RemComp<StunSlaveComponent>(target);
            _popup.PopupEntity(Loc.GetString("Цель может двигаться."), uid, uid);
        }

        args.Handled = true;

    }
    private void OnHeadUnitology(EntityUid uid, UnitologyHeadComponent component, UnitologyHeadActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        var target = args.Target;
        if (!IsCanTransfer(uid, target))
            return;

        args.Handled = true;

        RemComp<UnitologyHeadComponent>(uid);

        if (HasComp<UnitologyObeliskSpawnAbilityComponent>(uid))
        {
            RemComp<UnitologyObeliskSpawnAbilityComponent>(uid);
            AddComp<UnitologyObeliskSpawnAbilityComponent>(target);
        }

        AddComp<UnitologyHeadComponent>(target);
    }

    private bool IsCanTransfer(EntityUid uid, EntityUid target)
    {
        if (!HasComp<UnitologyComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель должна быть юнитологом!"), uid, uid);
            return false;
        }

        if (HasComp<UnitologyHeadComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель уже обладает вашими знаниями и положением!"), uid, uid);
            return false;
        }

        if (HasComp<UnitologyEnslavedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель не может быть порабощенным!"), uid, uid);
            return false;
        }

        if (HasComp<NecromorfComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель не может быть некроморфом!"), uid, uid);
            return false;
        }

        return true;
    }
}
