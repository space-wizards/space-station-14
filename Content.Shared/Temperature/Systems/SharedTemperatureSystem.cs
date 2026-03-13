using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// This handles predicting temperature based speedup.
/// </summary>
public abstract class SharedTemperatureSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    protected EntityQuery<TemperatureComponent> TemperatureQuery;

    /// <summary>
    /// Band-aid for unpredicted atmos. Delays the application for a short period so that laggy clients can get the replicated temperature.
    /// </summary>
    private static readonly TimeSpan SlowdownApplicationDelay = TimeSpan.FromSeconds(1f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TemperatureComponent, MassDataChangedEvent>(OnMassDataChanged);
        SubscribeLocalEvent<TemperatureSpeedComponent, TemperatureChangedEvent>(OnTemperatureChanged);
        SubscribeLocalEvent<TemperatureSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);

        TemperatureQuery = GetEntityQuery<TemperatureComponent>();
    }

    protected virtual void OnMapInit(Entity<TemperatureComponent> entity, ref MapInitEvent args)
    {
        var mass = CompOrNull<PhysicsComponent>(entity)?.FixturesMass ?? 1f;

        // TODO: This assumes this is temperature for the whole body, but ideally we want it split into surface and internal temperature!
        entity.Comp.HeatContainer.HeatCapacity = mass * entity.Comp.SpecificHeat;
    }

    private void OnMassDataChanged(Entity<TemperatureComponent> entity, ref MassDataChangedEvent args)
    {
        entity.Comp.HeatContainer.HeatCapacity = args.NewMass * entity.Comp.SpecificHeat;
    }

    private void OnTemperatureChanged(Entity<TemperatureSpeedComponent> ent, ref TemperatureChangedEvent args)
    {
        foreach (var (threshold, modifier) in ent.Comp.Thresholds)
        {
            if (args.CurrentTemperature < threshold && args.LastTemperature > threshold ||
                args.CurrentTemperature > threshold && args.LastTemperature < threshold)
            {
                ent.Comp.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
                ent.Comp.CurrentSpeedModifier = modifier;
                Dirty(ent);
                break;
            }
        }

        var maxThreshold = ent.Comp.Thresholds.Max(p => p.Key);
        if (args.CurrentTemperature > maxThreshold && args.LastTemperature < maxThreshold)
        {
            ent.Comp.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
            ent.Comp.CurrentSpeedModifier = null;
            Dirty(ent);
        }
    }

    private void OnRefreshMovementSpeedModifiers(Entity<TemperatureSpeedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // Don't update speed and mispredict while we're compensating for lag.
        if (ent.Comp.NextSlowdownUpdate != null || ent.Comp.CurrentSpeedModifier == null)
            return;

        args.ModifySpeed(ent.Comp.CurrentSpeedModifier.Value, ent.Comp.CurrentSpeedModifier.Value);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TemperatureSpeedComponent, MovementSpeedModifierComponent>();
        while (query.MoveNext(out var uid, out var temp, out var movement))
        {
            if (temp.NextSlowdownUpdate == null)
                continue;

            if (_timing.CurTime < temp.NextSlowdownUpdate)
                continue;

            temp.NextSlowdownUpdate = null;
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid, movement);
            Dirty(uid, temp);
        }
    }

    /// <inheritdoc cref="ConductHeat(Entity{TemperatureComponent?},ref HeatContainer,float,float,bool)"/>
    public float ConductHeat(Entity<TemperatureComponent?> entity, ref HeatContainer heatContainer, float conductanceMod = 1f, bool ignoreHeatResistance = false)
    {
        return ConductHeat(entity, ref heatContainer, (float)_timing.TickPeriod.TotalSeconds, conductanceMod, ignoreHeatResistance);
    }

    /// <summary>
    /// Conducts heat from one HeatContainer to another.
    /// Does nothing on client.
    /// </summary>
    /// <param name="entity">Entity we're conducting Heat with</param>
    /// <param name="heatContainer">Heat container which is conducting heat with our entity</param>
    /// <param name="deltaT">The length of this exchange in delta time</param>
    /// <param name="conductanceMod">An optional conductance modifier for this exchange</param>
    /// <param name="ignoreHeatResistance">Whether we should avoid raising an event to modify the conductance of our entity.</param>
    /// <returns>Returns the amount of heat exchanged</returns>
    public float ConductHeat(Entity<TemperatureComponent?> entity, ref HeatContainer heatContainer, float deltaT, float conductanceMod = 1f, bool ignoreHeatResistance = false)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp, false)
            || MathHelper.CloseTo(entity.Comp.HeatContainer.Temperature, heatContainer.Temperature))
            return 0f;

        var conductance = entity.Comp.ThermalConductance * conductanceMod;
        if (!ignoreHeatResistance)
        {
            var ev = new BeforeHeatExchangeEvent(conductance);
            RaiseLocalEvent(entity, ref ev);
            conductance = ev.Conductance;
        }

        var lastTemp = entity.Comp.CurrentTemperature;
        var heatEx = entity.Comp.HeatContainer.ConductHeat(ref heatContainer, deltaT, conductance);

        var changeEv = new TemperatureChangedEvent(entity.Comp.CurrentTemperature, lastTemp);
        RaiseLocalEvent(entity, ref changeEv, broadcast: true);
        return heatEx;
    }

    public float ConductHeat(Entity<TemperatureComponent?> entity, float temperature, float deltaT, float conductanceMod = 1f, bool ignoreHeatResistance = false)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp, false)
            || MathHelper.CloseTo(entity.Comp.HeatContainer.Temperature, temperature))
            return 0f;

        var conductance = entity.Comp.ThermalConductance * conductanceMod;
        if (!ignoreHeatResistance)
        {
            var ev = new BeforeHeatExchangeEvent(conductance);
            RaiseLocalEvent(entity, ref ev);
            conductance = ev.Conductance;
        }

        var lastTemp = entity.Comp.CurrentTemperature;
        var heatEx = entity.Comp.HeatContainer.ConductHeat(temperature, deltaT, conductance);

        var changeEv = new TemperatureChangedEvent(entity.Comp.CurrentTemperature, lastTemp);
        RaiseLocalEvent(entity, ref changeEv, broadcast: true);
        return heatEx;
    }

    /// <summary>
    /// Adds or removes the specified amount of heat from an entity.
    /// </summary>
    /// <param name="entity">Entity whose heat we're modifying.</param>
    /// <param name="heatAmount">The change in heat.</param>
    /// <param name="ignoreHeatResistance">Whether we should avoid raising an event to modify the conductance of our entity.</param>
    /// <returns>Returns the amount of heat exchanged</returns>
    public float ChangeHeat(Entity<TemperatureComponent?> entity, float heatAmount, bool ignoreHeatResistance = false)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp, false) || heatAmount == 0f)
            return 0f;

        var conductance = 1f;
        if (!ignoreHeatResistance)
        {
            var ev = new BeforeHeatExchangeEvent(conductance);
            RaiseLocalEvent(entity, ref ev );
            heatAmount *= ev.Conductance;
        }

        var lastTemp = entity.Comp.CurrentTemperature;
        entity.Comp.HeatContainer.AddHeat(heatAmount);

        var changeEv = new TemperatureChangedEvent(entity.Comp.CurrentTemperature, lastTemp);
        RaiseLocalEvent(entity, ref changeEv, broadcast: true);

        return heatAmount;
    }

    /// <summary>
    /// Protected because you should not be calling this. Sets heat to a new value.
    /// </summary>
    protected void SetTemperature(Entity<TemperatureComponent?> entity, float temp)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
            return;

        var lastTemp = entity.Comp.CurrentTemperature;
        entity.Comp.HeatContainer.Temperature = temp;
        var changeEv = new TemperatureChangedEvent(entity.Comp.CurrentTemperature, lastTemp);
        RaiseLocalEvent(entity, ref changeEv, broadcast: true);
    }
}
