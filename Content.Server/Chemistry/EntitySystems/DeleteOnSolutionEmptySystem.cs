using Content.Server.Chemistry.Components.DeleteOnSolutionEmptyComponent;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.Chemistry.EntitySystems.DeleteOnSolutionEmptySystem
{
    public sealed class DeleteOnSolutionEmptySystem : EntitySystem
    {
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DeleteOnSolutionEmptyComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DeleteOnSolutionEmptyComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        }

        public void OnStartup(Entity<DeleteOnSolutionEmptyComponent> entity, ref ComponentStartup args)
        {
            CheckSolutions(entity);
        }

        public void OnSolutionChange(Entity<DeleteOnSolutionEmptyComponent> entity, ref SolutionContainerChangedEvent args)
        {
            CheckSolutions(entity);
        }

        public void CheckSolutions(Entity<DeleteOnSolutionEmptyComponent> entity)
        {
            if (!TryComp(entity, out SolutionContainerManagerComponent? solutions))
                return;

            if (_solutionContainerSystem.TryGetSolution((entity.Owner, solutions), entity.Comp.Solution, out _, out var solution))
                if (solution.Volume <= 0)
                    EntityManager.QueueDeleteEntity(entity);
        }
    }
}
