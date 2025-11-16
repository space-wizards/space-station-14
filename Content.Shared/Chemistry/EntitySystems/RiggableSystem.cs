using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Kitchen;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rejuvenate;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Handles sabotaged/rigged objects.
/// </summary>
public sealed class RiggableSystem : EntitySystem
{
    [Dependency] private readonly SharedExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PredictedBatterySystem _predictedBattery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RiggableComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RiggableComponent, BeingMicrowavedEvent>(OnMicrowaved);
        SubscribeLocalEvent<RiggableComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<RiggableComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<RiggableComponent, PredictedBatteryChargeChangedEvent>(OnPredictedBatteryChargeChanged);
        SubscribeLocalEvent<RiggableComponent, ItemToggledEvent>(OnItemToggled);
    }

    private void OnRejuvenate(Entity<RiggableComponent> entity, ref RejuvenateEvent args)
    {
        entity.Comp.IsRigged = false;
        // TODO: This should probably also remove the reagent that rigged it.
    }

    private void OnMicrowaved(Entity<RiggableComponent> ent, ref BeingMicrowavedEvent args)
    {
        if (!TryExplodeBattery(ent) || TryExplodePredictedBattery(ent))
            args.Handled = true;
    }

    private void OnSolutionChanged(Entity<RiggableComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.Solution)
            return;

        var wasRigged = ent.Comp.IsRigged;
        var quantity = args.Solution.GetReagentQuantity(ent.Comp.RequiredQuantity.Reagent);
        ent.Comp.IsRigged = quantity >= ent.Comp.RequiredQuantity.Quantity;

        if (ent.Comp.IsRigged != wasRigged)
            Dirty(ent);

        if (ent.Comp.IsRigged && !wasRigged)
        {
            _adminLogger.Add(LogType.Explosion, LogImpact.Medium, $"{ToPrettyString(ent.Owner)} has been rigged up to explode when used.");
        }
    }

    /// <summary>
    /// Explode this entity with the given charge value.
    /// The explosion radius will scale with the battery charge.
    /// </summary>
    public void Explode(Entity<RiggableComponent> ent, float charge, EntityUid? cause = null)
    {
        if (ent.Comp.Exploded)
            return;

        var radius = MathF.Min(5, MathF.Sqrt(charge) / 9);

        _explosionSystem.TriggerExplosive(ent.Owner, radius: radius, user: cause);
        ent.Comp.Exploded = true;
        Dirty(ent);
        QueueDel(ent.Owner);
    }

    /// <summary>
    /// Try to explode this entity with the charge value from the entity's <see cref="BatteryComponent"/>.
    /// The explosion radius will scale with the battery charge.
    /// </summary>
    public bool TryExplodeBattery(Entity<RiggableComponent> ent)
    {
        if (!ent.Comp.IsRigged)
            return false;

        if (!TryComp<BatteryComponent>(ent, out var batteryComponent))
            return false;

        if (batteryComponent.CurrentCharge == 0f)
            return false;

        Explode(ent, batteryComponent.CurrentCharge);
        return true;
    }

    /// <summary>
    /// Try to explode this entity with the charge value from the entity's <see cref="PredictedBatteryComponent"/>.
    /// The explosion radius will scale with the battery charge.
    /// </summary>
    public bool TryExplodePredictedBattery(Entity<RiggableComponent> ent)
    {
        if (!ent.Comp.IsRigged)
            return false;

        if (!TryComp<PredictedBatteryComponent>(ent, out var predictedBatteryComponent))
            return false;

        var charge = _predictedBattery.GetCharge((ent.Owner, predictedBatteryComponent));
        if (charge == 0f)
            return false;

        Explode(ent, charge);

        return true;
    }

    // non-predicted batteries
    private void OnChargeChanged(Entity<RiggableComponent> ent, ref ChargeChangedEvent args)
    {
        TryExplodeBattery(ent);
    }

    // predicted batteries
    private void OnPredictedBatteryChargeChanged(Entity<RiggableComponent> ent, ref PredictedBatteryChargeChangedEvent args)
    {
        TryExplodePredictedBattery(ent);
    }

    // item toggle, like stun batons
    private void OnItemToggled(Entity<RiggableComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            return;

        if (!TryExplodeBattery(ent))
            TryExplodePredictedBattery(ent);
    }
}
