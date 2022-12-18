
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Body.Components;
using Content.Shared.Interaction;
using Content.Server.DoAfter;
using System.Threading;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Robust.Server.GameObjects;

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

            if(solution == null)
                return;
            if(!TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
                return;
            if(!args.CanReach)
                return;
            if(_foodSystem.IsMouthBlocked(args.Target.Value, args.User))
                return;
            if(args.Target == args.User)
                comp.Delay /= 2;
            
            var entMan = IoCManager.Resolve<IEntityManager>();

            //Always read the description.
            if(solution.ContainsReagent("Water"))
            {
                _explosionSystem.QueueExplosion(uid, "Default", 2.5f, 0.5f, 3, canCreateVacuum: false);
                entMan.DeleteEntity(uid);
            }

            comp.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, comp.Delay, comp.CancelToken.Token, args.Target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BroadcastFinishedEvent = new VapingEvent(args.User, args.Target.Value, uid, comp, solution, bloodstream)
            });
            args.Handled = true;
		}

        private void InjectAndSmokeReagents(VapingEvent args)
        {
            args.VapeComponent.CancelToken = null;

            _bloodstreamSystem.TryAddToChemicals(args.Target, args.Solution, args.Bloodstream);

            if(args.Solution.AvailableVolume < 3)
            {
                args.Solution.MaxVolume += 3;
                CreateSmoke(args.Item, args.Solution);
                args.Solution.MaxVolume -= 3;
            }
            else if(args.Solution.CurrentVolume != 0)
                CreateSmoke(args.Item, args.Solution);

            args.Solution.RemoveAllSolution();
        }

        //kinda shit, but i can't find function to create smoke.
        private void CreateSmoke(EntityUid uid, Solution solutions)
        {
            var smoke = new Solution();
            smoke.AddReagent("Phosphorus", 1); 
            smoke.AddReagent("Potassium", 1);
            smoke.AddReagent("Sugar", 1);
            
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