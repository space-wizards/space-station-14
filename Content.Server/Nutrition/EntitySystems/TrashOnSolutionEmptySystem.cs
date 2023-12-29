using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Tag;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed class TrashOnSolutionEmptySystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TrashOnSolutionEmptyComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<TrashOnSolutionEmptyComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        public void OnStartup(EntityUid uid, TrashOnSolutionEmptyComponent component, ComponentStartup args)
        {
            CheckSolutions(component);
        }

        public void OnSolutionChange(EntityUid uid, TrashOnSolutionEmptyComponent component, SolutionChangedEvent args)
        {
            CheckSolutions(component);
        }

        public void CheckSolutions(TrashOnSolutionEmptyComponent component)
        {
            if (!EntityManager.HasComponent<SolutionContainerManagerComponent>((component).Owner))
                return;

            if (_solutionContainerSystem.TryGetSolution(component.Owner, component.Solution, out var solution))
                UpdateTags(component, solution);
        }

        public void UpdateTags(TrashOnSolutionEmptyComponent component, Solution solution)
        {
            if (solution.Volume <= 0)
            {
                _tagSystem.AddTag(component.Owner, "Trash");
                return;
            }
            if (_tagSystem.HasTag(component.Owner, "Trash"))
                _tagSystem.RemoveTag(component.Owner, "Trash");
        }
    }
}
