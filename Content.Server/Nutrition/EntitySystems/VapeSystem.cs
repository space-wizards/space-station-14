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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VapeComponent, AfterInteractEvent>(OnInteraction);

            SubscribeLocalEvent<VapeComponent, VapingEvent>(InjectAndSmokeReagents);
            SubscribeLocalEvent<VapeComponent, VapingEventCancel>(OnInteractionCanceled);
        }

        private void OnInteraction(EntityUid uid, VapeComponent comp, AfterInteractEvent args) 
        {
            _solutionContainerSystem.TryGetRefillableSolution(uid, out var solution);

            var delay = comp.Delay;

            if (!args.CanReach
            || solution == null
            || comp.CancelToken != null
            || !TryComp<BloodstreamComponent>(args.Target, out var bloodstream)
            || _foodSystem.IsMouthBlocked(args.Target.Value, args.User))
                return;

            if (args.Target == args.User)
                delay = comp.UserDelay;

            //Always read the description.
            if (solution.ContainsReagent("Water") || comp.ExplodeOnUse)
            {
                _explosionSystem.QueueExplosion(uid, "Default", comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
                EntityManager.DeleteEntity(uid);
            }

            comp.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, delay, comp.CancelToken.Token, args.Target, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = false,
                BreakOnDamage = true,
                BreakOnStun = true,
                UsedFinishedEvent = new VapingEvent(args.User, args.Target.Value, solution, bloodstream),
                UsedCancelledEvent = new VapingEventCancel()
            });
            args.Handled = true;
		}

        private void InjectAndSmokeReagents(EntityUid uid, VapeComponent comp, VapingEvent args)
        {
            comp.CancelToken = null;

            if (args.Solution.CurrentVolume != 0)
                SmokeAreaReactionEffect.SpawnSmoke("Smoke",Transform(args.Target).Coordinates, args.Solution, comp.SmokeAmount, 5, 1, 1, entityManager: EntityManager);

            args.Solution.ScaleSolution(0.6f);

            //Smoking kills(your lungs, but there is no organ damage yet)
            _damageableSystem.TryChangeDamage(args.Target, comp.Damage, true);

            _bloodstreamSystem.TryAddToChemicals(args.Target, args.Solution, args.Bloodstream);

            args.Solution.RemoveAllSolution();
        }

        private void OnInteractionCanceled(EntityUid uid, VapeComponent comp, VapingEventCancel args)
        {
            comp.CancelToken = null;
        }
	}

    public sealed class VapingEvent : EntityEventArgs
    {
        public EntityUid User { get; }
        public EntityUid Target { get; }
        public Solution Solution { get; }
        public BloodstreamComponent Bloodstream;

        public VapingEvent(EntityUid user, EntityUid target, Solution solution, BloodstreamComponent bloodstream)
        {
            User = user;
            Target = target;
            Solution = solution;
            Bloodstream = bloodstream;
        }
    }

    public sealed class VapingEventCancel : EntityEventArgs {}
}