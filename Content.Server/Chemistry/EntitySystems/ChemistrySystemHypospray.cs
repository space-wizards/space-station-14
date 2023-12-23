using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Timing;
using Robust.Shared.GameStates;
using Content.Shared.Forensics;

namespace Content.Server.Chemistry.EntitySystems
{
    public sealed partial class ChemistrySystem
    {
        [Dependency] private readonly UseDelaySystem _useDelay = default!;

        private void InitializeHypospray()
        {
            SubscribeLocalEvent<HyposprayComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HyposprayComponent, MeleeHitEvent>(OnAttack);
            SubscribeLocalEvent<HyposprayComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<HyposprayComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<HyposprayComponent, ComponentGetState>(OnHypoGetState);
        }

        private void OnHypoGetState(EntityUid uid, HyposprayComponent component, ref ComponentGetState args)
        {
            args.State = _solutions.TryGetSolution(uid, component.SolutionName, out var solution)
                ? new HyposprayComponentState(solution.Volume, solution.MaxVolume)
                : new HyposprayComponentState(FixedPoint2.Zero, FixedPoint2.Zero);
        }

        private void OnUseInHand(EntityUid uid, HyposprayComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            TryDoInject(uid, args.User, args.User);
            args.Handled = true;
        }

        private void OnSolutionChange(EntityUid uid, HyposprayComponent component, SolutionChangedEvent args)
        {
            Dirty(component);
        }

        public void OnAfterInteract(EntityUid uid, HyposprayComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            var target = args.Target;
            var user = args.User;

            TryDoInject(uid, target, user);
        }

        public void OnAttack(EntityUid uid, HyposprayComponent component, MeleeHitEvent args)
        {
            if (!args.HitEntities.Any())
                return;

            TryDoInject(uid, args.HitEntities.First(), args.User);
        }

        public bool TryDoInject(EntityUid uid, EntityUid? target, EntityUid user, HyposprayComponent? component=null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!EligibleEntity(target, _entMan, component))
                return false;

            if (TryComp(uid, out UseDelayComponent? delayComp) && _useDelay.ActiveDelay(uid, delayComp))
                return false;

            string? msgFormat = null;

            if (target == user)
                msgFormat = "hypospray-component-inject-self-message";
            else if (EligibleEntity(user, _entMan, component) && _interaction.TryRollClumsy(user, component.ClumsyFailChance))
            {
                msgFormat = "hypospray-component-inject-self-clumsy-message";
                target = user;
            }

            _solutions.TryGetSolution(uid, component.SolutionName, out var hypoSpraySolution);

            if (hypoSpraySolution == null || hypoSpraySolution.Volume == 0)
            {
                _popup.PopupCursor(Loc.GetString("hypospray-component-empty-message"), user);
                return true;
            }

            if (!_solutions.TryGetInjectableSolution(target.Value, out var targetSolution))
            {
                _popup.PopupCursor(Loc.GetString("hypospray-cant-inject", ("target", Identity.Entity(target.Value, _entMan))), user);
                return false;
            }

            _popup.PopupCursor(Loc.GetString(msgFormat ?? "hypospray-component-inject-other-message", ("other", target)), user);

            if (target != user)
            {
                _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target.Value, target.Value);
                // TODO: This should just be using melee attacks...
                // meleeSys.SendLunge(angle, user);
            }

            _audio.PlayPvs(component.InjectSound, user);

            // Medipens and such use this system and don't have a delay, requiring extra checks
            // BeginDelay function returns if item is already on delay
            if (delayComp is not null)
                _useDelay.BeginDelay(uid, delayComp);

            // Get transfer amount. May be smaller than component.TransferAmount if not enough room
            var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.AvailableVolume);

            if (realTransferAmount <= 0)
            {
                _popup.PopupCursor(Loc.GetString("hypospray-component-transfer-already-full-message",("owner", target)), user);
                return true;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = _solutions.SplitSolution(uid, hypoSpraySolution, realTransferAmount);

            if (!targetSolution.CanAddSolution(removedSolution))
                return true;
            _reactiveSystem.DoEntityReaction(target.Value, removedSolution, ReactionMethod.Injection);
            _solutions.TryAddSolution(target.Value, targetSolution, removedSolution);

            var ev = new TransferDnaEvent { Donor = target.Value, Recipient = uid };
            RaiseLocalEvent(target.Value, ref ev);

            // same LogType as syringes...
            _adminLogger.Add(LogType.ForceFeed, $"{_entMan.ToPrettyString(user):user} injected {_entMan.ToPrettyString(target.Value):target} with a solution {SolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {_entMan.ToPrettyString(uid):using}");

            return true;
        }

        static bool EligibleEntity([NotNullWhen(true)] EntityUid? entity, IEntityManager entMan, HyposprayComponent component)
        {
            // TODO: Does checking for BodyComponent make sense as a "can be hypospray'd" tag?
            // In SS13 the hypospray ONLY works on mobs, NOT beakers or anything else.
            // But this is 14, we dont do what SS13 does just because SS13 does it.
            return component.OnlyMobs
                ? entMan.HasComponent<SolutionContainerManagerComponent>(entity) &&
                  entMan.HasComponent<MobStateComponent>(entity)
                : entMan.HasComponent<SolutionContainerManagerComponent>(entity);
        }
    }
}
