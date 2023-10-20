using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
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

        public void OnStartup(Entity<TrashOnEmptyComponent> ent, ref ComponentStartup args)
        {
            CheckSolutions(ent);
        }

        public void OnSolutionChange(Entity<TrashOnEmptyComponent> ent, ref SolutionChangedEvent args)
        {
            CheckSolutions(ent);
        }

        public void CheckSolutions(Entity<TrashOnEmptyComponent> ent)
        {
            if (!HasComp<SolutionContainerManagerComponent>(ent))
                return;

            if (_solutionContainerSystem.TryGetSolution(ent, ent.Comp.Solution, out var solution))
                UpdateTags(ent, solution);
        }

        public void UpdateTags(Entity<TrashOnEmptyComponent> ent, Solution solution)
        {
            if (solution.Volume <= 0)
            {
                _tagSystem.AddTag(ent, "Trash");
                return;
            }
            if (_tagSystem.HasTag(ent, "Trash"))
                _tagSystem.RemoveTag(ent, "Trash");
        }
    }
}
