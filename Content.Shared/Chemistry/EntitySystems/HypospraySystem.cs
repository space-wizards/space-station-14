using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class HypospraySystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyposprayComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HyposprayComponent, MeleeHitEvent>(OnAttack);
        SubscribeLocalEvent<HyposprayComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HyposprayComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleModeVerb);
        SubscribeLocalEvent<HyposprayComponent, HyposprayDrawDoAfterEvent>(OnDrawDoAfter);
    }

    #region Ref events
    private void OnUseInHand(Entity<HyposprayComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryDoInject(entity, args.User, args.User);
    }

    private void OnAfterInteract(Entity<HyposprayComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = TryUseHypospray(entity, args.Target.Value, args.User);
    }

    private void OnAttack(Entity<HyposprayComponent> entity, ref MeleeHitEvent args)
    {
        if (args.HitEntities is [])
            return;

        TryDoInject(entity, args.HitEntities[0], args.User);
    }

    private void OnDrawDoAfter(Entity<HyposprayComponent> entity, ref HyposprayDrawDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (entity.Comp.CanContainerDraw
            && args.Target.HasValue
            && !EligibleEntity(args.Target.Value, entity)
            && _solutionContainers.TryGetDrawableSolution(args.Target.Value, out var drawableSolution, out _))
        {
            TryDraw(entity, args.Target.Value, drawableSolution.Value, args.User);
        }
    }

    #endregion

    #region Draw/Inject
    private bool TryUseHypospray(Entity<HyposprayComponent> entity, EntityUid target, EntityUid user)
    {
        // if target is ineligible but is a container, try to draw from the container if allowed
        if (entity.Comp.CanContainerDraw
            && !EligibleEntity(target, entity)
            && _solutionContainers.TryGetDrawableSolution(target, out var drawableSolution, out _))
        {
            return TryStartDraw(entity, target, drawableSolution.Value, user);
        }

        return TryDoInject(entity, target, user);
    }

    public bool TryDoInject(Entity<HyposprayComponent> entity, EntityUid target, EntityUid user)
    {
        var (uid, component) = entity;

        if (!EligibleEntity(target, component))
            return false;

        if (TryComp(uid, out UseDelayComponent? delayComp))
        {
            if (_useDelay.IsDelayed((uid, delayComp)))
                return false;
        }

        string? msgFormat = null;

        // Self event
        var selfEvent = new SelfBeforeHyposprayInjectsEvent(user, entity.Owner, target);
        RaiseLocalEvent(user, selfEvent);

        if (selfEvent.Cancelled)
        {
            _popup.PopupClient(Loc.GetString(selfEvent.InjectMessageOverride ?? "hypospray-cant-inject", ("owner", Identity.Entity(target, EntityManager))), target, user);
            return false;
        }

        target = selfEvent.TargetGettingInjected;

        if (!EligibleEntity(target, component))
            return false;

        // Target event
        var targetEvent = new TargetBeforeHyposprayInjectsEvent(user, entity.Owner, target);
        RaiseLocalEvent(target, targetEvent);

        if (targetEvent.Cancelled)
        {
            _popup.PopupClient(Loc.GetString(targetEvent.InjectMessageOverride ?? "hypospray-cant-inject", ("owner", Identity.Entity(target, EntityManager))), target, user);
            return false;
        }

        target = targetEvent.TargetGettingInjected;

        if (!EligibleEntity(target, component))
            return false;

        // The target event gets priority for the overriden message.
        if (targetEvent.InjectMessageOverride != null)
            msgFormat = targetEvent.InjectMessageOverride;
        else if (selfEvent.InjectMessageOverride != null)
            msgFormat = selfEvent.InjectMessageOverride;
        else if (target == user)
            msgFormat = "hypospray-component-inject-self-message";

        if (!_solutionContainers.TryGetSolution(uid, component.SolutionName, out var hypoSpraySoln, out var hypoSpraySolution) || hypoSpraySolution.Volume == 0)
        {
            _popup.PopupClient(Loc.GetString("hypospray-component-empty-message"), target, user);
            return true;
        }

        if (!_solutionContainers.TryGetInjectableSolution(target, out var targetSoln, out var targetSolution))
        {
            _popup.PopupClient(Loc.GetString("hypospray-cant-inject", ("target", Identity.Entity(target, EntityManager))), target, user);
            return false;
        }

        _popup.PopupClient(Loc.GetString(msgFormat ?? "hypospray-component-inject-other-message", ("other", Identity.Entity(target, EntityManager))), target, user);

        if (target != user)
        {
            _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
            // TODO: This should just be using melee attacks...
            // meleeSys.SendLunge(angle, user);
        }

        _audio.PlayPredicted(component.InjectSound, target, user);

        // Medipens and such use this system and don't have a delay, requiring extra checks
        // BeginDelay function returns if item is already on delay
        if (delayComp != null)
            _useDelay.TryResetDelay((uid, delayComp));

        // Get transfer amount. May be smaller than component.TransferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupClient(Loc.GetString("hypospray-component-transfer-already-full-message", ("owner", target)), target, user);
            return true;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutionContainers.SplitSolution(hypoSpraySoln.Value, realTransferAmount);

        if (!targetSolution.CanAddSolution(removedSolution))
            return true;
        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);
        _solutionContainers.TryAddSolution(targetSoln.Value, removedSolution);

        var ev = new TransferDnaEvent { Donor = target, Recipient = uid };
        RaiseLocalEvent(target, ref ev);

        // same LogType as syringes...
        _adminLogger.Add(LogType.ForceFeed, $"{ToPrettyString(user):user} injected {ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {ToPrettyString(uid):using}");

        return true;
    }

    public bool TryStartDraw(Entity<HyposprayComponent> entity, EntityUid target, Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!_solutionContainers.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var soln))
            return false;

        if (!TryGetDrawAmount(entity, target, targetSolution, user,  soln.Value, out _))
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.DrawTime, new HyposprayDrawDoAfterEvent(), entity, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            Hidden = true,
        };

        return _doAfter.TryStartDoAfter(doAfterArgs, out _);
    }

    private bool TryGetDrawAmount(Entity<HyposprayComponent> entity, EntityUid target, Entity<SolutionComponent> targetSolution, EntityUid user, Entity<SolutionComponent> solutionEntity, [NotNullWhen(true)] out FixedPoint2? amount)
    {
        amount = null;

        if (solutionEntity.Comp.Solution.AvailableVolume == 0)
        {
            return false;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(entity.Comp.TransferAmount, targetSolution.Comp.Solution.Volume,
            solutionEntity.Comp.Solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupClient(
                Loc.GetString("injector-component-target-is-empty-message",
                    ("target", Identity.Entity(target, EntityManager))),
                entity.Owner, user);
            return false;
        }

        amount = realTransferAmount;
        return true;
    }

    private bool TryDraw(Entity<HyposprayComponent> entity, EntityUid target, Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!_solutionContainers.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var soln))
            return false;

        if (!TryGetDrawAmount(entity, target, targetSolution, user, soln.Value, out var amount))
            return false;

        var removedSolution = _solutionContainers.Draw(target, targetSolution, amount.Value);

        if (!_solutionContainers.TryAddSolution(soln.Value, removedSolution))
        {
            return false;
        }

        _popup.PopupClient(Loc.GetString("injector-component-draw-success-message",
            ("amount", removedSolution.Volume),
            ("target", Identity.Entity(target, EntityManager))), entity.Owner, user);
        return true;
    }

    private bool EligibleEntity(EntityUid entity, HyposprayComponent component)
    {
        // TODO: Does checking for BodyComponent make sense as a "can be hypospray'd" tag?
        // In SS13 the hypospray ONLY works on mobs, NOT beakers or anything else.
        // But this is 14, we dont do what SS13 does just because SS13 does it.
        return component.OnlyAffectsMobs
            ? HasComp<SolutionContainerManagerComponent>(entity) &&
              HasComp<MobStateComponent>(entity)
            : HasComp<SolutionContainerManagerComponent>(entity);
    }

    #endregion

    #region Verbs

    // <summary>
    // Uses the OnlyMobs field as a check to implement the ability
    // to draw from jugs and containers with the hypospray
    // Toggleable to allow people to inject containers if they prefer it over drawing
    // </summary>
    private void AddToggleModeVerb(Entity<HyposprayComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || entity.Comp.InjectOnly)
            return;

        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("hypospray-verb-mode-label"),
            Act = () =>
            {
                ToggleMode(entity, user);
            }
        };
        args.Verbs.Add(verb);
    }

    private void ToggleMode(Entity<HyposprayComponent> entity, EntityUid user)
    {
        SetMode(entity, !entity.Comp.OnlyAffectsMobs);
        var msg = (entity.Comp.OnlyAffectsMobs && entity.Comp.CanContainerDraw) ? "hypospray-verb-mode-inject-mobs-only" : "hypospray-verb-mode-inject-all";
        _popup.PopupClient(Loc.GetString(msg), entity, user);
    }

    public void SetMode(Entity<HyposprayComponent> entity, bool onlyAffectsMobs)
    {
        if (entity.Comp.OnlyAffectsMobs == onlyAffectsMobs)
            return;

        entity.Comp.OnlyAffectsMobs = onlyAffectsMobs;
        Dirty(entity);
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class HyposprayDrawDoAfterEvent : SimpleDoAfterEvent {}
