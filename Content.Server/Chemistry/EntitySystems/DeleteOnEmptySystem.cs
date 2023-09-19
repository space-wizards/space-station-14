using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.Components;

namespace Content.Server.Chemistry.EntitySystems
{
    public sealed class DeleteOnEmptySystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DeleteOnEmptyComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DeleteOnEmptyComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        public void OnStartup(EntityUid uid, DeleteOnEmptyComponent component, ComponentStartup args)
        {
            CheckSolutions(uid, component);
        }

        public void OnSolutionChange(EntityUid uid, DeleteOnEmptyComponent component, SolutionChangedEvent args)
        {
            CheckSolutions(uid, component);
        }

        public void CheckSolutions(EntityUid uid, DeleteOnEmptyComponent component)
        {
            if (!EntityManager.HasComponent<SolutionContainerManagerComponent>((component).Owner))
                return;

            if (_solutionContainerSystem.TryGetSolution(component.Owner, component.Solution, out var solution))
                if (solution.Volume <= 0)
                    EntityManager.QueueDeleteEntity(uid);
        }
    }
}
