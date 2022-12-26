using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed class TrashOnEmptySystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TrashOnEmptyComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<TrashOnEmptyComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        public void OnStartup(EntityUid uid, TrashOnEmptyComponent component, ComponentStartup args)
        {
            CheckSolutions(component);
        }

        public void OnSolutionChange(EntityUid uid, TrashOnEmptyComponent component, SolutionChangedEvent args)
        {
            CheckSolutions(component);
        }

        public void CheckSolutions(TrashOnEmptyComponent component)
        {
            if (!EntityManager.HasComponent<SolutionContainerManagerComponent>((component).Owner))
                return;

            if (_solutionContainerSystem.TryGetSolution(component.Owner, component.Solution, out var solution))
                UpdateTags(component, solution);
        }

        public void UpdateTags(TrashOnEmptyComponent component, Solution solution)
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
