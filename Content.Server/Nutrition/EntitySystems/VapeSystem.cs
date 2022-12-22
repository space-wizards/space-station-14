
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Body.Components;
using Content.Shared.Interaction;
using Content.Server.DoAfter;
using System.Threading;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed class VapeSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly FoodSystem _foodSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VapeComponent, AfterInteractEvent>(OnInteraction);

            SubscribeLocalEvent<VapingEvent>(InjectAndSmokeReagents);
        }

        private void OnInteraction(EntityUid uid, VapeComponent comp, AfterInteractEvent args) 
        {
            _solutionContainerSystem.TryGetRefillableSolution(uid, out var solution);

            var delay = comp.Delay;

            if(!TryComp<BloodstreamComponent>(args.Target, out var bloodstream)
            || solution == null
            || !args.CanReach
            || _foodSystem.IsMouthBlocked(args.Target.Value, args.User))
                return;

            if(args.Target == args.User)
                delay = comp.UserDelay;
            
            var entMan = IoCManager.Resolve<IEntityManager>();

            //Always read the description.
            if(solution.ContainsReagent("Water") || comp.ExplodeOnUse)
            {
                _explosionSystem.QueueExplosion(uid, "Default", comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
                entMan.DeleteEntity(uid);
            }

            comp.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, delay, comp.CancelToken.Token, args.Target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = false,
                BreakOnDamage = true,
                BreakOnStun = true,
                BroadcastFinishedEvent = new VapingEvent(args.User, args.Target.Value, uid, comp, solution, bloodstream)
            });
            args.Handled = true;
		}

        private void InjectAndSmokeReagents(VapingEvent args)
        {
            args.VapeComponent.CancelToken = null;

            if(args.Solution.AvailableVolume <= args.VapeComponent.SmokeAmount * 3)
            {
                args.Solution.MaxVolume += args.VapeComponent.SmokeAmount * 3;

                CreateSmoke(args.Item, args.Solution, args.VapeComponent);

                args.Solution.MaxVolume -= args.VapeComponent.SmokeAmount * 3;
            }
            else if(args.Solution.CurrentVolume != 0)
                CreateSmoke(args.Item, args.Solution, args.VapeComponent);

            args.Solution.RemoveSolution(args.Solution.CurrentVolume / 2);

            //Smoking kills(your lungs, but there is no organ damage yet)
            args.Solution.AddReagent("Thermite", 1);

            _bloodstreamSystem.TryAddToChemicals(args.Target, args.Solution, args.Bloodstream);

            args.Solution.RemoveAllSolution();
        }

        //kinda shit, but i can't find function to create smoke.
        private void CreateSmoke(EntityUid uid, Solution solutions, VapeComponent comp)
        {
            var smoke = new Solution();
            smoke.AddReagent("Phosphorus", comp.SmokeAmount); 
            smoke.AddReagent("Potassium", comp.SmokeAmount);
            smoke.AddReagent("Sugar", comp.SmokeAmount);
            
            _solutionContainerSystem.TryAddSolution(uid, solutions, smoke);
        }
	}

    internal sealed class VapingEvent : EntityEventArgs
    {
        public EntityUid User { get; }
        public EntityUid Target { get; }
        public EntityUid Item { get; }
        public VapeComponent VapeComponent;
        public Solution Solution { get; }
        public BloodstreamComponent Bloodstream;

        public VapingEvent(EntityUid user, EntityUid target, EntityUid item, VapeComponent vapeComponent, Solution solution, BloodstreamComponent bloodstream)
        {
            User = user;
            Target = target;
            Item = item;
            VapeComponent = vapeComponent;
            Solution = solution;
            Bloodstream = bloodstream;
        }
    }
}