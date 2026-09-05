using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Kitchen;
using Content.Shared.Rejuvenate;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
///  Handles sabotaged/rigged objects
/// </summary>
public sealed partial class RiggableSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedExplosionSystem _explosionSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<RiggableComponent> entity, ref RejuvenateEvent args)
    {
        if (!_solution.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solution, true))
            return;

        _solution.RemoveAllSolution(solution.Value);
        entity.Comp.IsRigged = false;
        DirtyField(entity, entity.Comp, nameof(RiggableComponent.IsRigged));
    }

    [SubscribeLocalEvent]
    private void OnMicrowaved(Entity<RiggableComponent> entity, ref BeingMicrowavedEvent args)
    {
        if (!entity.Comp.IsRigged)
            return;

        var charge = _battery.GetCharge(entity.Owner);
        Explode(entity, charge, args.User);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSolutionChanged(Entity<RiggableComponent> entity, ref SolutionChangedEvent args)
    {
        if (args.Solution.Comp.Id != entity.Comp.Solution)
            return;

        var wasRigged = entity.Comp.IsRigged;
        var solution = args.Solution.Comp.Solution;
        var quantity = solution.GetReagentQuantity(entity.Comp.Reagent.Reagent);
        entity.Comp.IsRigged = quantity >= entity.Comp.Reagent.Quantity;

        if (wasRigged || !entity.Comp.IsRigged)
            return;

        _adminLogger.Add(LogType.Explosion, LogImpact.Medium, $"{ToPrettyString(entity)} has been rigged up to explode when used.");

        if (!TryComp<ItemToggleComponent>(entity, out var toggleComp) || !toggleComp.Activated)
            return;

        Explode(entity, _battery.GetCharge(entity.Owner));
    }

    [SubscribeLocalEvent]
    private void OnChargeChanged(Entity<RiggableComponent> entity, ref ChargeChangedEvent args)
    {
        if (!entity.Comp.IsRigged)
            return;

        if (args.CurrentCharge == 0f)
            return; // No charge to cause an explosion.

        // Don't explode if we are not using any charge.
        if (args.CurrentChargeRate == 0f && args.Delta == 0f)
            return;

        Explode(entity, args.CurrentCharge);
    }

    [SubscribeLocalEvent]
    private void OnToggled(Entity<RiggableComponent> entity, ref ItemToggledEvent args)
    {
        if (!args.Activated || !entity.Comp.IsRigged)
            return;

        Explode(entity, _battery.GetCharge(entity.Owner), args.User);
    }

    public void Explode(Entity<RiggableComponent> entity, float charge, EntityUid? cause = null)
    {
        if (entity.Comp.Exploded || charge == 0f)
            return;

        var radius = MathF.Min(5, MathF.Sqrt(charge) / 9);

        // Explosion system also queues entity deletion
        _explosionSystem.TriggerExplosive(entity, radius: radius, user: cause);

        entity.Comp.Exploded = true;
        DirtyField(entity, entity.Comp, nameof(RiggableComponent.Exploded));
    }
}
