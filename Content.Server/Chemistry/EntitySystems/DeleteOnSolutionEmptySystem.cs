using Content.Server.Chemistry.Components.DeleteOnSolutionEmptyComponent;
using Content.Server.Chemistry.Containers.EntitySystems;
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
            SubscribeLocalEvent<DeleteOnSolutionEmptyComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        }

        public void OnStartup(EntityUid uid, DeleteOnSolutionEmptyComponent component, ComponentStartup args)
        {
            CheckSolutions(uid, component);
        }

        public void OnSolutionChange(EntityUid uid, DeleteOnSolutionEmptyComponent component, SolutionContainerChangedEvent args)
        {
            CheckSolutions(uid, component);
        }

        public void CheckSolutions(EntityUid uid, DeleteOnSolutionEmptyComponent component)
        {
            if (!EntityManager.HasComponent<SolutionContainerComponent>(uid))
                return;

            if (_solutionContainerSystem.TryGetSolution(uid, component.Solution, out _, out var solution))
                if (solution.Volume <= 0)
                    EntityManager.QueueDeleteEntity(uid);
        }
    }
}
