// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Server.DeadSpace.Sith.Components;
using Content.Shared.Popups;
using Content.Shared.DeadSpace.Sith;
using Content.Shared.DoAfter;
using Content.Shared.DeadSpace.Sith.Components;
using Content.Server.Mind;
using Content.Server.Revolutionary.Components;
using Content.Shared.Examine;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.DeadSpace.Sith;

public sealed class SithSubmissionAbilitySystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SithSubmissionAbilityComponent, SithSubmissionEvent>(OnSithSubmission);
        SubscribeLocalEvent<SithSubmissionAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SithSubmissionAbilityComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SithSubmissionAbilityComponent, SithSubmissionDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SithSubmissionAbilityComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, SithSubmissionAbilityComponent component, ExaminedEvent args)
    {
        if (args.Examiner == args.Examined)
        {
            args.PushMarkup(Loc.GetString($"Рабов в подчинении: [color=red]{component.Submissions}, не считая командования.[/color]."));
        }
    }

    private void OnComponentInit(EntityUid uid, SithSubmissionAbilityComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionSithSubmissionEntity, component.ActionSithSubmission, uid);
    }

    private void OnComponentShutdown(EntityUid uid, SithSubmissionAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionSithSubmissionEntity);
    }

    private void OnSithSubmission(EntityUid uid, SithSubmissionAbilityComponent component, SithSubmissionEvent args)
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

        var searchDoAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(component.Duration), new SithSubmissionDoAfterEvent(), uid, target: target)
        {
            Broadcast = true,
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, SithSubmissionAbilityComponent component, SithSubmissionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (!IsCanSubmission(uid, target, component))
            return;

        if (HasComp<SithSubordinateComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Существо больше не под контролем!"), uid, uid);
            RemComp<SithSubordinateComponent>(target);
            return;
        }

        var subComponent = new SithSubordinateComponent
        {
            Master = uid
        };

        if (!HasComp<CommandStaffComponent>(target))
            component.Submissions += 1;

        EntityManager.AddComponent(target, subComponent);
    }

    private bool IsCanSubmission(EntityUid uid, EntityUid target, SithSubmissionAbilityComponent component)
    {
        if (HasComp<SithSubordinateComponent>(target))
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
