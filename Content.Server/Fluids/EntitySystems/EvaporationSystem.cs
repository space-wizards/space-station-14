using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public sealed class EvaporationSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EvaporationComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, EvaporationComponent component, ComponentInit args)
        {
            component.Accumulator = 0f;
            _solutionContainerSystem.EnsureSolution(uid, component.SolutionName);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var evaporationComponent in ComponentManager.EntityQuery<EvaporationComponent>())
            {
                UpdateEvaporation(evaporationComponent, frameTime);
            }
        }

        private void UpdateEvaporation(EvaporationComponent evaporationComponent, float frameTime)
        {
            evaporationComponent.Accumulator += frameTime;

            if (evaporationComponent.Accumulator < evaporationComponent.EvaporateTime)
                return;

            evaporationComponent.Accumulator -= evaporationComponent.EvaporateTime;

            var uid = evaporationComponent.Owner.Uid;
            var solution = _solutionContainerSystem.GetSolution(uid, evaporationComponent.SolutionName);

            if (evaporationComponent.EvaporationLimit >= solution.CurrentVolume)
            {
                ComponentManager.RemoveComponent<EvaporationComponent>(uid);
                return;
            }


            _solutionContainerSystem.SplitSolution(uid, solution,
                ReagentUnit.Min(ReagentUnit.New(1), solution.CurrentVolume));

            RaiseLocalEvent(uid, new SolutionChangedEvent());

            if (solution.CurrentVolume == 0)
            {
                EntityManager.QueueDeleteEntity(uid);
            }


        }
    }
}
