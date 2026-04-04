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
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RiggableComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RiggableComponent, BeingMicrowavedEvent>(OnMicrowaved);
        SubscribeLocalEvent<RiggableComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<RiggableComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnRejuvenate(Entity<RiggableComponent> entity, ref RejuvenateEvent args)
    {
        entity.Comp.IsRigged = false;
    }

    private void OnMicrowaved(Entity<RiggableComponent> entity, ref BeingMicrowavedEvent args)
    {
        if (TryComp<BatteryComponent>(entity, out var batteryComponent))
        {
            var charge = _battery.GetCharge((entity, batteryComponent));
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

    private void OnChargeChanged(Entity<RiggableComponent> ent, ref ChargeChangedEvent args)
    {
        if (!ent.Comp.IsRigged)
            return;

        if (args.CurrentCharge == 0f)
            return; // No charge to cause an explosion.

        // Don't explode if we are not using any charge.
        if (args.CurrentChargeRate == 0f && args.Delta == 0f)
            return;

        Explode(ent, args.CurrentCharge);
    }
}
