using Content.Server.PowerCell.Components;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.PowerCell
{
    [UsedImplicitly]
    public class PowerCellSystem  : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PowerCellComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, PowerCellComponent component, SolutionChangedEvent args)
        {
            component.IsRigged =  _solutionsSystem.TryGetSolution(uid, PowerCellComponent.SolutionName, out var solution)
                                   && solution.ContainsReagent("Plasma", out var plasma)
                                   && plasma >= 5;
        }
    }
}
