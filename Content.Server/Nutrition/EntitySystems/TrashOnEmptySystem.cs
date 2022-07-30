using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
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
            SubscribeLocalEvent<TrashOnEmptyComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, TrashOnEmptyComponent component, SolutionChangedEvent args)
        {
            if (!EntityManager.HasComponent<SolutionContainerManagerComponent>((component).Owner))
                return;
            if (_tagSystem.HasTag(component.Owner, "Trash"))
                return;
            if (!_solutionContainerSystem.TryGetDrainableSolution(component.Owner, out var drinkSolution) || drinkSolution.DrainAvailable <= 0)
            {
                EntityManager.EnsureComponent<TagComponent>(component.Owner);
                _tagSystem.AddTag(component.Owner, "Trash");
            }
        }
    }
}
