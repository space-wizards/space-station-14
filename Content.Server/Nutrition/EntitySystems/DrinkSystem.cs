using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;


namespace Content.Server.Nutrition.EntitySystems;

public sealed class DrinkSystem : SharedDrinkSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO add InteractNoHandEvent for entities like mice.
        SubscribeLocalEvent<DrinkComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
        // run before inventory so for bucket it always tries to drink before equipping (when empty)
        // run after openable so its always open -> drink
    }

    private void OnDrinkInit(Entity<DrinkComponent> entity, ref ComponentInit args)
    {
        if (TryComp<DrainableSolutionComponent>(entity, out var existingDrainable))
        {
            // Beakers have Drink component but they should use the existing Drainable
            entity.Comp.Solution = existingDrainable.Solution;
        }
        else
        {
            _solutionContainer.EnsureSolution(entity.Owner, entity.Comp.Solution, out _);
        }

        UpdateAppearance(entity, entity.Comp);

        if (TryComp(entity, out RefillableSolutionComponent? refillComp))
            refillComp.Solution = entity.Comp.Solution;

        if (TryComp(entity, out DrainableSolutionComponent? drainComp))
            drainComp.Solution = entity.Comp.Solution;
    }

    private void OnSolutionChange(Entity<DrinkComponent> entity, ref SolutionContainerChangedEvent args)
    {
        UpdateAppearance(entity, entity.Comp);
    }

    public void UpdateAppearance(EntityUid uid, DrinkComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !HasComp<SolutionContainerManagerComponent>(uid))
        {
            return;
        }

        var drainAvailable = DrinkVolume(uid, component);
        _appearance.SetData(uid, FoodVisuals.Visual, drainAvailable.Float(), appearance);
    }
}
