using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio.Systems;
using Content.Shared.NPC.Systems;
using Content.Server.Body.Components;
using Content.Shared.FixedPoint;
using Content.Shared.DeadSpace.Abilities.Bloodsucker;
using Content.Shared.DoAfter;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Server.DeadSpace.Abilities.Cocoon.Components;
using Content.Server.DeadSpace.Spiders.SpiderTerror.Components;
using Content.Server.DeadSpace.Spiders.SpiderTerror;

namespace Content.Server.DeadSpace.Abilities.Bloodsucker;

public sealed partial class BloodsuckerSystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SpiderTerrorTombSystem _tomb = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodsuckerComponent, SuckBloodActionEvent>(DoSuckBlood);
        SubscribeLocalEvent<BloodsuckerComponent, BloodsuckerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<BloodsuckerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodsuckerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BloodsuckerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnMapInit(EntityUid uid, BloodsuckerComponent component, MapInitEvent args)
    {
        UpdateBloodAlert(uid, component);
    }
    private void OnComponentInit(EntityUid uid, BloodsuckerComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionSuckBloodEntity, component.ActionSuckBlood, uid);
    }

    private void OnShutdown(EntityUid uid, BloodsuckerComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionSuckBloodEntity);
    }

    private void OnExamine(EntityUid uid, BloodsuckerComponent component, ExaminedEvent args)
    {
        if (args.Examiner == args.Examined)
        {
            args.PushMarkup(Loc.GetString("Вы содержите [color=red]" + component.CountReagent.ToString() + " крови[/color]. Из [color=red]"
            + component.MaxCountReagent.ToString() + "[/color] возможных."));
        }
    }
    private void DoSuckBlood(EntityUid uid, BloodsuckerComponent component, SuckBloodActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        var target = args.Target;

        if (TryComp<SpiderTerrorTombComponent>(target, out var spiderTerrorTombComp))
        {
            if (component.HowMuchWillItSuck <= spiderTerrorTombComp.Reagent)
            {
                if (component.CountReagent >= component.MaxCountReagent)
                {
                    _popup.PopupEntity(Loc.GetString("Вы сыты"), uid, uid);
                    return;
                }
                BeginSuck(uid, target, component);
                return;
            }
        }

        if (TryComp<CocoonComponent>(target, out var cocoonComponent))
        {
            if (cocoonComponent.Stomach.ContainedEntities.Count > 0)
            {
                var firstEntity = cocoonComponent.Stomach.ContainedEntities[0];
                target = firstEntity;
            }
        }

        if (component.CountReagent >= component.MaxCountReagent)
        {
            _popup.PopupEntity(Loc.GetString("Вы сыты"), uid, uid);
            return;
        }

        bool isCanSuck = false;

        if (!TryComp<MobStateComponent>(target, out var mobState))
            return;

        foreach (var allowedState in component.AllowedStates)
        {
            if (allowedState == mobState.CurrentState)
            {
                isCanSuck = !isCanSuck;
                break;
            }
        }

        if (!isCanSuck)
            return;

        if (_npcFaction.IsEntityFriendly(uid, target))
            return;

        if (!HasComp<BodyComponent>(target))
            return;

        if (!_solutionContainer.TryGetInjectableSolution(target, out var injectable, out _))
            return;

        BeginSuck(uid, target, component);

    }

    private void BeginSuck(EntityUid uid, EntityUid target, BloodsuckerComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(component.Duration), new BloodsuckerDoAfterEvent(), uid, target: target)
        {
            Broadcast = true,
            DistanceThreshold = 2,
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, BloodsuckerComponent component, BloodsuckerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (TryComp<SpiderTerrorTombComponent>(target, out var spiderTerrorTombComp))
        {
            if (component.HowMuchWillItSuck <= spiderTerrorTombComp.Reagent)
            {
                _tomb.AddReagent(target, -component.HowMuchWillItSuck, spiderTerrorTombComp);
                SetReagentCount(uid, component.HowMuchWillItSuck);

                if (component.InjectSound != null)
                    _audio.PlayPvs(component.InjectSound, target);

                return;
            }
        }

        var bloodPercentage = _bloodstreamSystem.GetBloodLevelPercentage(target);
        if (bloodPercentage <= 0.05)
        {
            _popup.PopupEntity(Loc.GetString("У цели нет питательных веществ"), uid, uid);
            return;
        }

        if (!EntityManager.TryGetComponent(target, out BloodstreamComponent? bloodstreamComponent))
            return;

        if (!TryModifyBloodLevel(target, -component.HowMuchWillItSuck, bloodstreamComponent))
            return;

        if (component.InjectSound != null)
            _audio.PlayPvs(component.InjectSound, target);

        _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);

        SetReagentCount(uid, component.HowMuchWillItSuck, component);
        _popup.PopupEntity(Loc.GetString("У вас есть ") + component.CountReagent.ToString() + Loc.GetString(" питательных веществ"), uid, uid);

    }

    private bool TryModifyBloodLevel(EntityUid uid, FixedPoint2 amount, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (!_solutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution))
            return false;

        if (amount >= 0)
            return _solutionContainerSystem.TryAddReagent(component.BloodSolution.Value, component.BloodReagent, amount, out _);

        var newSol = _solutionContainerSystem.SplitSolution(component.BloodSolution.Value, -amount);

        if (!_solutionContainerSystem.ResolveSolution(uid, component.BloodTemporarySolutionName, ref component.TemporarySolution, out var tempSolution))
            return true;


        _solutionContainerSystem.UpdateChemicals(component.TemporarySolution.Value);

        return true;
    }

}
