using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Body.Components;
using Content.Shared.Interaction;
using Content.Server.DoAfter;
using System.Threading;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Damage;
using Content.Server.Chemistry.ReactionEffects;
using Content.Server.Popups;
using Robust.Shared.Player;
using Content.Shared.IdentityManagement;

namespace Content.Server.Nutrition.Vape
{
    public sealed class VapeSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly FoodSystem _foodSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly FlavorProfileSystem _flavorProfileSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VapeComponent, AfterInteractEvent>(OnInteraction);

            SubscribeLocalEvent<VapeComponent, VapingEvent>(InjectAndSmokeReagents);
            SubscribeLocalEvent<VapingEventCancel>(OnInteractionCanceled);
        }

        private void OnInteraction(EntityUid uid, VapeComponent comp, AfterInteractEvent args) 
        {
            _solutionContainerSystem.TryGetRefillableSolution(uid, out var solution);

            var delay = comp.Delay;
            var forced = true;

            if (!args.CanReach
            || solution == null
            || comp.CancelToken != null
            || !TryComp<BloodstreamComponent>(args.Target, out var bloodstream)
            || _foodSystem.IsMouthBlocked(args.Target.Value, args.User))
                return;

            if (args.Target == args.User){
                delay = comp.UserDelay;
                forced = false;
            }

            //Always read the description.
            if (solution.ContainsReagent("Water") || comp.ExplodeOnUse)
            {
                _explosionSystem.QueueExplosion(uid, "Default", comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
                EntityManager.DeleteEntity(uid);
            }

            if (forced)
            {
                var targetName = Identity.Entity(args.Target.Value, EntityManager);
                var userName = Identity.Entity(args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced", ("user", userName)), args.Target.Value,
                    Filter.Entities(args.Target.Value));
                
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced-user", ("target", targetName)), args.User,
                    Filter.Entities(args.User));
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape"), args.User,
                    Filter.Entities(args.User));
            }

            comp.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, delay, comp.CancelToken.Token, args.Target, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = false,
                BreakOnDamage = true,
                BreakOnStun = true,
                UsedFinishedEvent = new VapingEvent(args.User, args.Target.Value, solution, bloodstream, forced),
                BroadcastCancelledEvent = new VapingEventCancel(comp)
            });
            args.Handled = true;
		}

        private void InjectAndSmokeReagents(EntityUid uid, VapeComponent comp, VapingEvent args)
        {
            comp.CancelToken = null;

            var flavors = _flavorProfileSystem.GetLocalizedFlavorsMessage(args.User, args.Solution);

            if (args.Solution.CurrentVolume != 0)
                SmokeAreaReactionEffect.SpawnSmoke(comp.SmokePrototype, Transform(args.Target).Coordinates,
                    args.Solution, comp.SmokeAmount, 5, 1, 1, entityManager: EntityManager);

            args.Solution.ScaleSolution(0.6f);

            //Smoking kills(your lungs, but there is no organ damage yet)
            _damageableSystem.TryChangeDamage(args.Target, comp.Damage, true);

            _bloodstreamSystem.TryAddToChemicals(args.Target, args.Solution, args.Bloodstream);

            args.Solution.RemoveAllSolution();
            
            if (args.Forced)
            {
                var targetName = Identity.Entity(args.Target, EntityManager);
                var userName = Identity.Entity(args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-taste-forced", ("flavors", flavors), ("user", userName)), args.Target,
                    Filter.Entities(args.Target));
                
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-user-forced", ("target", targetName)), args.User,
                    Filter.Entities(args.User));
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-taste", ("flavors", flavors)), args.Target,
                    Filter.Entities(args.Target));
            }
        }

        private void OnInteractionCanceled(VapingEventCancel args)
        {
            args.VapeComponent.CancelToken = null;
        }
	}

    public sealed class VapingEvent : EntityEventArgs
    {
        public EntityUid User { get; }
        public EntityUid Target { get; }
        public Solution Solution { get; }
        public bool Forced { get; }
        public BloodstreamComponent Bloodstream;

        public VapingEvent(EntityUid user, EntityUid target, Solution solution, BloodstreamComponent bloodstream, bool forced)
        {
            User = user;
            Target = target;
            Solution = solution;
            Bloodstream = bloodstream;
            Forced = forced;
        }
    }

    public sealed class VapingEventCancel : EntityEventArgs 
    {
        public VapeComponent VapeComponent;
        public VapingEventCancel(VapeComponent comp)
        {
            VapeComponent = comp;
        }
    }
}