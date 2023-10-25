using Content.Server.Chemistry.Components.DeleteOnSolutionEmptyComponent;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.Chemistry.EntitySystems.DeleteOnSolutionEmptySystem
{
    public sealed class DeleteOnSolutionEmptySystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DeleteOnSolutionEmptyComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DeleteOnSolutionEmptyComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        public void OnStartup(EntityUid uid, DeleteOnSolutionEmptyComponent component, ComponentStartup args)
        {
            CheckSolutions(uid, component);
        }

        public void OnSolutionChange(EntityUid uid, DeleteOnSolutionEmptyComponent component, SolutionChangedEvent args)
        {
            CheckSolutions(uid, component);
        }

        public void CheckSolutions(EntityUid uid, DeleteOnSolutionEmptyComponent component)
        {
            if (!EntityManager.HasComponent<SolutionContainerManagerComponent>(uid))
                return;

            if (_solutionContainerSystem.TryGetSolution(uid, component.Solution, out var solution))
                if (solution.Volume <= 0)
                    EntityManager.QueueDeleteEntity(uid);
        }
    }
}
