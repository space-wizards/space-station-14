using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed class TrashOnEmptySystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TrashOnEmptyComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, TrashOnEmptyComponent component, SolutionChangedEvent args)
        {
            EntityManager.EnsureComponent<TagComponent>(component.Owner);

            if (!EntityManager.HasComponent<SolutionContainerManagerComponent>((component).Owner))
                return;

            if (_solutionContainerSystem.TryGetDrainableSolution(component.Owner, out var solution))
                UpdateTags(component, solution);
            // have to do this because syringes don't have drainable solution OR injectable solution for some reason :(
            if (_solutionContainerSystem.TryGetSolution(component.Owner, "injector", out var injectableSolution))
                UpdateTags(component, injectableSolution);
        }

        private void UpdateTags(TrashOnEmptyComponent component, Solution solution)
        {
            if (solution.DrainAvailable <= 0)
            {
                _tagSystem.AddTag(component.Owner, "Trash");
                return;
            }
            if (_tagSystem.HasTag(component.Owner, "Trash"))
                _tagSystem.RemoveTag(component.Owner, "Trash");
        }
    }
}
