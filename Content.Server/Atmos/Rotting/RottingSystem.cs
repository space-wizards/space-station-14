using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Temperature.Components;
using Robust.Server.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Rotting;

public sealed partial class RottingSystem : SharedRottingSystem
{
    private static readonly EntityTimerId PerishTimer = new("perish");
    private static readonly EntityTimerId RotTimer = new("rot");

    [Dependency] private EntityTimerSystem _timers = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RottingComponent, GibbedBeforeDeletionEvent>(OnGibbed);

        SubscribeLocalEvent<TemperatureComponent, IsRottingEvent>(OnTempIsRotting);
        SubscribeLocalEvent<PerishableComponent, ComponentStartup>(OnPerishableStartup);
        SubscribeLocalEvent<PerishableComponent, EntityTimerEvent>(OnPerishTimer);
        SubscribeLocalEvent<RottingComponent, ComponentStartup>(OnRottingStartup);
        SubscribeLocalEvent<RottingComponent, EntityTimerEvent>(OnRotTimer);
    }

    private void OnPerishableStartup(Entity<PerishableComponent> ent, ref ComponentStartup args)
    {
        _timers.SetTimerAt(ent, PerishTimer, ent.Comp.RotNextUpdate);
    }

    private void OnRottingStartup(Entity<RottingComponent> ent, ref ComponentStartup args)
    {
        _timers.SetTimerAt(ent, RotTimer, ent.Comp.NextRotUpdate);
    }

    private void OnGibbed(EntityUid uid, RottingComponent component, GibbedBeforeDeletionEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        if (!TryComp<PerishableComponent>(uid, out var perishable))
            return;

        var molsToDump = perishable.MolsPerSecondPerUnitMass * physics.FixturesMass * (float)component.TotalRotTime.TotalSeconds;
        var tileMix = _atmosphere.GetTileMixture(uid, excite: true);
        tileMix?.AdjustMoles(Gas.Ammonia, molsToDump);
    }

    private void OnTempIsRotting(EntityUid uid, TemperatureComponent component, ref IsRottingEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = component.CurrentTemperature < Atmospherics.T0C + 0.85f;
    }

    /// <summary>
    /// Is anything speeding up the decay?
    /// e.g. buried in a grave
    /// TODO: hot temperatures increase rot?
    /// </summary>
    /// <returns></returns>
    private float GetRotRate(EntityUid uid)
    {
        if (_container.TryGetContainingContainer((uid, null, null), out var container) &&
            TryComp<ProRottingContainerComponent>(container.Owner, out var rotContainer))
        {
            return rotContainer.DecayModifier;
        }

        return 1f;
    }

    private void OnPerishTimer(Entity<PerishableComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != PerishTimer)
            return;

        var perishable = ent.Comp;
        perishable.RotNextUpdate = args.ScheduledTime + perishable.PerishUpdateRate;
        _timers.SetTimerAt(ent, PerishTimer, perishable.RotNextUpdate);

        var stage = PerishStage(ent, MaxStages);
        if (stage != perishable.Stage)
        {
            perishable.Stage = stage;
            DirtyField(ent, perishable, nameof(PerishableComponent.Stage));
        }

        if (IsRotten(ent) || !IsRotProgressing(ent, perishable))
            return;

        perishable.RotAccumulator += perishable.PerishUpdateRate * GetRotRate(ent);
        DirtyField(ent, perishable, nameof(PerishableComponent.RotAccumulator));
        if (perishable.RotAccumulator < perishable.RotAfter)
            return;

        var rot = AddComp<RottingComponent>(ent);
        var ev = new BeginRottingEvent();
        RaiseLocalEvent(ent, ref ev);
        rot.NextRotUpdate = args.FiredAt + rot.RotUpdateRate;
        _timers.SetTimerAt<RottingComponent>((ent, rot), RotTimer, rot.NextRotUpdate);
    }

    private void OnRotTimer(Entity<RottingComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != RotTimer ||
            !TryComp<PerishableComponent>(ent, out var perishable) ||
            !TryComp<TransformComponent>(ent, out var xform))
            return;

        var rotting = ent.Comp;
        rotting.NextRotUpdate = args.ScheduledTime + rotting.RotUpdateRate;
        _timers.SetTimerAt(ent, RotTimer, rotting.NextRotUpdate);

        if (!IsRotProgressing(ent, perishable))
            return;
        rotting.TotalRotTime += rotting.RotUpdateRate * GetRotRate(ent);

        if (rotting.DealDamage)
        {
            var damage = rotting.Damage * rotting.RotUpdateRate.TotalSeconds;
            _damageable.TryChangeDamage(ent.Owner, damage, true, false);
        }

        if (TryComp<RotIntoComponent>(ent, out var rotInto))
        {
            var stage = RotStage(ent, rotting, perishable);
            if (stage >= rotInto.Stage)
            {
                Spawn(rotInto.Entity, xform.Coordinates);
                QueueDel(ent);
                return;
            }
        }

        if (!TryComp<PhysicsComponent>(ent, out var physics))
            return;

        // We need a way to get the mass of the mob alone without armor etc in the future
        // or just remove the mass mechanics altogether because they aren't good.
        var molRate = perishable.MolsPerSecondPerUnitMass * (float)rotting.RotUpdateRate.TotalSeconds;
        _atmosphere.AdjustTileMixture(ent.Owner, Gas.Ammonia, molRate * physics.FixturesMass, excite: true);
    }
}
