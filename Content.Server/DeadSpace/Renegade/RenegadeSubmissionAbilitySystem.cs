// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Server.DeadSpace.Renegade.Components;
using Content.Shared.Popups;
using Content.Shared.DeadSpace.Renegade;
using Content.Shared.DoAfter;
using Content.Shared.DeadSpace.Renegade.Components;
using Content.Server.Mind;
using Content.Server.Revolutionary.Components;
using Content.Shared.Examine;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.DeadSpace.Renegade;

public sealed class RenegadeSubmissionAbilitySystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenegadeSubmissionAbilityComponent, RenegadeSubmissionEvent>(OnRenegadeSubmission);
        SubscribeLocalEvent<RenegadeSubmissionAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RenegadeSubmissionAbilityComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<RenegadeSubmissionAbilityComponent, RenegadeSubmissionDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RenegadeSubmissionAbilityComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, RenegadeSubmissionAbilityComponent component, ExaminedEvent args)
    {
        if (args.Examiner == args.Examined)
        {
            args.PushMarkup(Loc.GetString($"Рабов в подчинении: [color=red]{component.Submissions}, не считая командования.[/color]."));
        }
    }

    private void OnComponentInit(EntityUid uid, RenegadeSubmissionAbilityComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionRenegadeSubmissionEntity, component.ActionRenegadeSubmission, uid);
    }

    private void OnComponentShutdown(EntityUid uid, RenegadeSubmissionAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionRenegadeSubmissionEntity);
    }

    private void OnRenegadeSubmission(EntityUid uid, RenegadeSubmissionAbilityComponent component, RenegadeSubmissionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (target == uid)
            return;

        if (!IsCanSubmission(uid, target, component))
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("Вы чувствуете сильное влияние на ваш разум!"), uid, target);

        var searchDoAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(component.Duration), new RenegadeSubmissionDoAfterEvent(), uid, target: target)
        {
            Broadcast = true,
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, RenegadeSubmissionAbilityComponent component, RenegadeSubmissionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (!IsCanSubmission(uid, target, component))
            return;

        if (HasComp<RenegadeSubordinateComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Существо больше не под контролем!"), uid, uid);
            RemComp<RenegadeSubordinateComponent>(target);
            return;
        }

        var subComponent = new RenegadeSubordinateComponent
        {
            Master = uid
        };

        if (!HasComp<CommandStaffComponent>(target))
            component.Submissions += 1;

        EntityManager.AddComponent(target, subComponent);
    }

    private bool IsCanSubmission(EntityUid uid, EntityUid target, RenegadeSubmissionAbilityComponent component)
    {
        if (HasComp<RenegadeSubordinateComponent>(target))
            return true;

        if (!HasComp<CommandStaffComponent>(target))
        {
            if (component.MaxSubmission <= component.Submissions)
            {
                _popup.PopupEntity(Loc.GetString("Вы достигли лимита рабов, командование не в счёт!"), uid, uid);
                return false;
            }
        }

        if (_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("Вы не можете подчинить мертвеца!"), uid, uid);
            return false;
        }

        if (HasComp<BorgChassisComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Вы не можете подчинить борга!"), uid, uid);
            return false;
        }

        if (!_mind.TryGetMind(target, out var mindId, out var mindComponent))
        {
            _popup.PopupEntity(Loc.GetString("Существо не обладает разумом!"), uid, uid);
            return false;
        }

        return true;
    }
}
