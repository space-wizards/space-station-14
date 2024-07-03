using Content.Shared.Atmos;
using Content.Shared.Inventory;
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

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<TemperatureProtectionComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(OnTemperatureChangeAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // conduct heat from the surface to the inside of entities with internal temperatures
        var query = EntityQueryEnumerator<InternalTemperatureComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var comp, out var temp))
        {
            // don't do anything if they equalised
            var diff = Math.Abs(temp.CurrentTemperature - comp.Temperature);
            if (diff < 0.1f)
                continue;

            // heat flow in W/m^2 as per fourier's law in 1D.
            var q = comp.Conductivity * diff / comp.Thickness;

            // convert to J then K
            var joules = q * comp.Area * frameTime;
            var degrees = joules / GetHeatCapacity((uid, temp));
            if (temp.CurrentTemperature < comp.Temperature)
                degrees *= -1;

            // exchange heat between inside and surface
            comp.Temperature += degrees;
            SetTemperature((uid, temp), temp.CurrentTemperature - degrees);
        }
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

    /// <summary>
    /// Sets the thermal energy of an entity to a new value.
    /// </summary>
    /// <param name="entity">The entity to set the thermal energy of.</param>
    /// <param name="value">The new thermal energy for the entity.</param>
    public void SetThermalEnergy(Entity<TemperatureComponent?> entity, float value)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
            return;

        var heatCapacity = GetHeatCapacity(entity);
        SetTemperature(entity, heatCapacity > 0 ? value / heatCapacity : 0);
    }

    /// <summary>
    /// Heats or cools an entity by some amount of thermal energy.
    /// </summary>
    /// <param name="entity">The entity to heat or cool.</param>
    /// <param name="delta">The amount of heat/thermal energy to add to or remove from the entity.</param>
    /// <param name="ignoreHeatResistance">Whether to ignore the entities thermal insulation when doing so.</param>
    public void AdjustThermalEnergy(Entity<TemperatureComponent?> entity, float delta, bool ignoreHeatResistance = false)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
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

    /// <summary>
    /// Gets the current themral energy of an entity.
    /// </summary>
    /// <param name="entity">The entity to get the thermal energy of.</param>
    /// <returns>The current thermal energy of the entity.</returns>
    public float GetThermalEnergy(Entity<TemperatureComponent?> entity)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
            return 0;

        return GetHeatCapacity(entity) * entity.Comp.CurrentTemperature;
    }

    /// <summary>
    /// Ensures the internal and external temperature of entities match at mapinit.
    /// </summary>
    private void OnMapInit(Entity<InternalTemperatureComponent> entity, ref MapInitEvent args)
    {
        if (!TemperatureQuery.TryGetComponent(entity, out var temperature))
        {
            Log.Warning($"{ToPrettyString(entity)} has an internal temperature without a {nameof(TemperatureComponent)}");
            return;
        }

        entity.Comp.Temperature = temperature.CurrentTemperature;
    }

    /// <summary>
    /// Resets the temperature of rejuvenated entities to a sane value.
    /// </summary>
    private void OnRejuvenate(Entity<TemperatureComponent> entity, ref RejuvenateEvent args)
    {
        SetTemperature((entity, entity.Comp), Atmospherics.T20C);
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnTemperatureChangeAttempt(Entity<TemperatureProtectionComponent> entity, ref InventoryRelayedEvent<ModifyChangedTemperatureEvent> args)
    {
        var ev = new GetTemperatureProtectionEvent(entity.Comp.Coefficient);
        RaiseLocalEvent(entity, ref ev);

        args.Args.TemperatureDelta *= ev.Coefficient;
    }
}

[ByRefEvent]
public readonly record struct OnTemperatureChangeEvent(float CurrentTemperature, float LastTemperature, Entity<TemperatureComponent> Entity)
{
    public readonly float TemperatureDelta = CurrentTemperature - LastTemperature;
}
