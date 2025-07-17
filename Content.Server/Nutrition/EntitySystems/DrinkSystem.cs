using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class DrinkSystem : SharedDrinkSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly ReactiveSystem _reaction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly ForensicsSystem _forensics = default!;

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
