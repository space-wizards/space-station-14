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
            SubscribeLocalEvent<DeleteOnSolutionEmptyComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        public void OnStartup(Entity<DeleteOnSolutionEmptyComponent> entity, ref ComponentStartup args)
        {
            CheckSolutions(entity);
        }

        public void OnSolutionChange(Entity<DeleteOnSolutionEmptyComponent> entity, ref SolutionChangedEvent args)
        {
            var solution = args.Solution.Comp.Solution;
            if (args.Solution.Comp.Id != entity.Comp.Solution)
                return;

            if (solution.Volume <= 0)
                QueueDel(entity);
        }

        public void CheckSolutions(Entity<DeleteOnSolutionEmptyComponent> entity)
        {
            if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
                return;

            if (solution.Volume <= 0)
                QueueDel(entity);
        }
    }
}
