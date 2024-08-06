using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Interaction;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Robust.Shared.GameStates;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Server.Audio;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class HypospraySystem : SharedHypospraySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyposprayComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HyposprayComponent, MeleeHitEvent>(OnAttack);
        SubscribeLocalEvent<HyposprayComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HyposprayComponent, HyposprayDoAfterEvent>(OnHyposprayDoAfter);
    }
    private void OnHyposprayDoAfter(Entity<HyposprayComponent> entity, ref HyposprayDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        args.Handled = TryDoInject(entity, args.Args.Target.Value, args.Args.User);
    }

    private bool TryUseHypospray(Entity<HyposprayComponent> entity, EntityUid target, EntityUid user)
    {
        // if target is ineligible but is a container, try to draw from the container
        if (!EligibleEntity(target, EntityManager, entity)
            && _solutionContainers.TryGetDrawableSolution(target, out var drawableSolution, out _))
        {
            return TryDraw(entity, target, drawableSolution.Value, user);
        }

        // No doAfter for self-injection
        if (entity.Comp.DoAfterTime > 0 && target != user)
        {
            // Is the target a mob? If yes, use a do-after to give them time to respond.
            if (HasComp<MobStateComponent>(target) || HasComp<BloodstreamComponent>(target))
            {
                //If the injection would fail the doAfter can be skipped at this step
                if (InjectionFailureCheck(entity, target, user, out _, out _, out _, out _))
                {
                    _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, entity.Comp.DoAfterTime, new HyposprayDoAfterEvent(), entity.Owner, target: target, used: entity.Owner)
                    {
                        BreakOnMove = true,
                        BreakOnWeightlessMove = false,
                        BreakOnDamage = true,
                        NeedHand = true,
                        BreakOnHandChange = true,
                        Hidden = true
                    });
                }
                return true;
            }
        }

        return TryDoInject(entity, target, user);
    }

    private void OnUseInHand(Entity<HyposprayComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryDoInject(entity, args.User, args.User);
    }

    public void OnAfterInteract(Entity<HyposprayComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = TryUseHypospray(entity, args.Target.Value, args.User);
    }

    public void OnAttack(Entity<HyposprayComponent> entity, ref MeleeHitEvent args)
    {
        if (!args.HitEntities.Any())
            return;

        TryDoInject(entity, args.HitEntities.First(), args.User);
    }

    public bool TryDoInject(Entity<HyposprayComponent> entity, EntityUid target, EntityUid user)
    {
        var (uid, component) = entity;

        if (!EligibleEntity(target, EntityManager, component))
            return false;

        if (TryComp(uid, out UseDelayComponent? delayComp))
        {
            if (_useDelay.IsDelayed((uid, delayComp)))
                return false;
        }

        string? msgFormat = null;

        if (target == user)
            msgFormat = "hypospray-component-inject-self-message";
        else if (EligibleEntity(user, EntityManager, component) && _interaction.TryRollClumsy(user, component.ClumsyFailChance))
        {
            msgFormat = "hypospray-component-inject-self-clumsy-message";
            target = user;
        }

        if (!InjectionFailureCheck(entity, target, user, out var hypoSpraySoln, out var targetSoln, out var targetSolution, out var returnValue)
            || hypoSpraySoln == null
            || targetSoln == null
            || targetSolution == null)
            return returnValue;

        _popup.PopupEntity(Loc.GetString(msgFormat ?? "hypospray-component-inject-other-message", ("other", target)), target, user);

        if (target != user)
        {
            _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
            // TODO: This should just be using melee attacks...
            // meleeSys.SendLunge(angle, user);
        }

        _audio.PlayPvs(component.InjectSound, user);

        // Medipens and such use this system and don't have a delay, requiring extra checks
        // BeginDelay function returns if item is already on delay
        if (delayComp != null)
            _useDelay.TryResetDelay((uid, delayComp));

        // Get transfer amount. May be smaller than component.TransferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("hypospray-component-transfer-already-full-message", ("owner", target)), target, user);
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
        _adminLogger.Add(LogType.ForceFeed, $"{EntityManager.ToPrettyString(user):user} injected {EntityManager.ToPrettyString(target):target} with a solution {SolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {EntityManager.ToPrettyString(uid):using}");

        return true;
    }

    private bool TryDraw(Entity<HyposprayComponent> entity, Entity<BloodstreamComponent?> target, Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!_solutionContainers.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var soln,
                out var solution) || solution.AvailableVolume == 0)
        {
            return false;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(entity.Comp.TransferAmount, targetSolution.Comp.Solution.Volume,
            solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(
                Loc.GetString("injector-component-target-is-empty-message",
                    ("target", Identity.Entity(target, EntityManager))),
                entity.Owner, user);
            return false;
        }

        var removedSolution = _solutionContainers.Draw(target.Owner, targetSolution, realTransferAmount);

        if (!_solutionContainers.TryAddSolution(soln.Value, removedSolution))
        {
            return false;
        }

        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
            ("amount", removedSolution.Volume),
            ("target", Identity.Entity(target, EntityManager))), entity.Owner, user);
        return true;
    }

    private bool EligibleEntity(EntityUid entity, IEntityManager entMan, HyposprayComponent component)
    {
        // TODO: Does checking for BodyComponent make sense as a "can be hypospray'd" tag?
        // In SS13 the hypospray ONLY works on mobs, NOT beakers or anything else.
        // But this is 14, we dont do what SS13 does just because SS13 does it.
        return component.OnlyAffectsMobs
            ? entMan.HasComponent<SolutionContainerManagerComponent>(entity) &&
              entMan.HasComponent<MobStateComponent>(entity)
            : entMan.HasComponent<SolutionContainerManagerComponent>(entity);
    }

    private bool InjectionFailureCheck(Entity<HyposprayComponent> entity, EntityUid target, EntityUid user, out Entity<SolutionComponent>? hypoSpraySoln, out Entity<SolutionComponent>? targetSoln, out Solution? targetSolution, out bool returnValue)
    {
        hypoSpraySoln = null;
        targetSoln = null;
        targetSolution = null;
        returnValue = false;

        if (!_solutionContainers.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out hypoSpraySoln, out var hypoSpraySolution) || hypoSpraySolution.Volume == 0)
        {
            _popup.PopupEntity(Loc.GetString("hypospray-component-empty-message"), target, user);
            returnValue = true;
            return false;
        }

        if (!_solutionContainers.TryGetInjectableSolution(target, out targetSoln, out targetSolution))
        {
            _popup.PopupEntity(Loc.GetString("hypospray-cant-inject", ("target", Identity.Entity(target, EntityManager))), target, user);
            returnValue = false;
            return false;
        }

        return true;
    }
}
