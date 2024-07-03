using Content.Shared.Atmos;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature.Components;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// The system responsible for managing entity temperatures and heat capacities.
/// </summary>
public abstract partial class SharedTemperatureSystem : EntitySystem
{
    private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    protected EntityQuery<TemperatureComponent> TemperatureQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        TemperatureQuery = GetEntityQuery<TemperatureComponent>();

        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
    }

    /// <summary>
    /// Sets the temperature of an entity to a new value.
    /// </summary>
    /// <param name="entity">The entity to modify the temperature of.</param>
    /// <param name="value">The temperature to set the entity to.</param>
    public void SetTemperature(Entity<TemperatureComponent?> entity, float value)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
            return;

        var lastTemp = entity.Comp.CurrentTemperature;
        entity.Comp.CurrentTemperature = value;

        var ev = new OnTemperatureChangeEvent(value, lastTemp, (entity.Owner, entity.Comp));
        RaiseLocalEvent(entity, ref ev);
    }

    public void SetThermalEnergy(Entity<TemperatureComponent?> entity, float value)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        var heatCapacity = GetHeatCapacity(entity);
        SetTemperature(entity, heatCapacity > 0 ? value / heatCapacity : 0);
    }

    public void AdjustThermalEnergy(Entity<TemperatureComponent?> entity, float delta, bool ignoreHeatResistance = false)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        var heatCapacity = GetHeatCapacity(entity);
        if (heatCapacity <= 0)
            return;

        if (!ignoreHeatResistance)
        {
            var ev = new ModifyChangedTemperatureEvent(delta);
            RaiseLocalEvent(entity, ev);
            delta = ev.TemperatureDelta;
        }

        SetTemperature(entity, entity.Comp.CurrentTemperature + delta / heatCapacity);
    }

    /// <summary>
    /// Gets the current heat capacity of an entity.
    /// </summary>
    /// <param name="entity">The entity to fetch the heat capacity of.</param>
    /// <returns>The current heat capacity of the given entity.</returns>
    public float GetHeatCapacity(Entity<TemperatureComponent?> entity)
    {
        var (uid, temperature) = entity;

        if (!TemperatureQuery.Resolve(uid, ref temperature) || !_physicsQuery.TryGetComponent(uid, out var physics) || physics.FixturesMass <= 0)
            return Atmospherics.MinimumHeatCapacity;

        return temperature.SpecificHeat * physics.FixturesMass;
    }

    public float GetThermalEnergy(Entity<TemperatureComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return 0;

        return GetHeatCapacity(entity) * entity.Comp.CurrentTemperature;
    }

    private void OnRejuvenate(Entity<TemperatureComponent> entity, ref RejuvenateEvent args)
    {
        SetTemperature((entity, entity.Comp), Atmospherics.T20C);
    }
}

[ByRefEvent]
public readonly record struct OnTemperatureChangeEvent(float CurrentTemperature, float LastTemperature, Entity<TemperatureComponent> Entity)
{
    public readonly float TemperatureDelta = CurrentTemperature - LastTemperature;
}
