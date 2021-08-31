using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public class DrinkSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DrinkComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
        }

        private void OnDrinkInit(EntityUid uid, DrinkComponent component, ComponentInit args)
        {
            component.Opened = component.DefaultToOpened;

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetComponent(out DrainableSolutionComponent? existingDrainable))
            {
                // Beakers have Drink component but they should use the existing Drainable
                component.SolutionName = existingDrainable.Solution;
            }
            else
            {
                _solutionContainerSystem.EnsureSolution(owner, component.SolutionName);
            }

            component.UpdateAppearance();
        }


        private void OnSolutionChange(EntityUid uid, DrinkComponent component, SolutionChangedEvent args)
        {
            component.UpdateAppearance();
        }
    }
}
