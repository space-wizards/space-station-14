using Content.Server.Administration.Logs;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Kitchen;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rejuvenate;

namespace Content.Server.Power.EntitySystems;

/// <summary>
///  Handles sabotaged/rigged objects
/// </summary>
public sealed class RiggableSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PredictedBatterySystem _predictedBattery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RiggableComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RiggableComponent, BeingMicrowavedEvent>(OnMicrowaved);
        SubscribeLocalEvent<RiggableComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<RiggableComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<RiggableComponent, PredictedBatteryChargeChangedEvent>(OnChargeChanged);
    }

    private void OnRejuvenate(Entity<RiggableComponent> entity, ref RejuvenateEvent args)
    {
        entity.Comp.IsRigged = false;
    }

    private void OnMicrowaved(Entity<RiggableComponent> entity, ref BeingMicrowavedEvent args)
    {
        if (TryComp<BatteryComponent>(entity, out var batteryComponent))
        {
            if (batteryComponent.CurrentCharge == 0f)
                return;

            Explode(entity, batteryComponent.CurrentCharge);
            args.Handled = true;
        }

        if (TryComp<PredictedBatteryComponent>(entity, out var predictedBatteryComponent))
        {
            var charge = _predictedBattery.GetCharge((entity, predictedBatteryComponent));
            if (charge == 0f)
                return;

            Explode(entity, charge);
            args.Handled = true;
        }
    }

    private void OnSolutionChanged(Entity<RiggableComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != entity.Comp.Solution)
            return;

        var wasRigged = entity.Comp.IsRigged;
        var quantity = args.Solution.GetReagentQuantity(entity.Comp.RequiredQuantity.Reagent);
        entity.Comp.IsRigged = quantity >= entity.Comp.RequiredQuantity.Quantity;

        if (entity.Comp.IsRigged && !wasRigged)
        {
            _adminLogger.Add(LogType.Explosion, LogImpact.Medium, $"{ToPrettyString(entity.Owner)} has been rigged up to explode when used.");
        }
    }

    public void Explode(EntityUid uid, float charge, EntityUid? cause = null)
    {
        var radius = MathF.Min(5, MathF.Sqrt(charge) / 9);

        _explosionSystem.TriggerExplosive(uid, radius: radius, user: cause);
        QueueDel(uid);
    }

    // non-predicted batteries
    private void OnChargeChanged(Entity<RiggableComponent> ent, ref ChargeChangedEvent args)
    {
        if (!ent.Comp.IsRigged)
            return;

        if (TryComp<BatteryComponent>(ent, out var batteryComponent))
        {
            if (batteryComponent.CurrentCharge == 0f)
                return;

            Explode(ent, batteryComponent.CurrentCharge);
        }
    }

    // predicted batteries
    private void OnChargeChanged(Entity<RiggableComponent> ent, ref PredictedBatteryChargeChangedEvent args)
    {
        if (!ent.Comp.IsRigged)
            return;

        if (TryComp<PredictedBatteryComponent>(ent, out var predictedBatteryComponent))
        {
            var charge = _predictedBattery.GetCharge((ent.Owner, predictedBatteryComponent));
            if (charge == 0f)
                return;

            Explode(ent, charge);
        }
    }
}
