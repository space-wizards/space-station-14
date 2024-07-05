using Content.Shared.Atmos;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// The system responsible for managing entity temperatures and heat capacities.
/// </summary>
public abstract partial class SharedTemperatureSystem : EntitySystem
{
    [Dependency] protected readonly IViewVariablesManager Vvm = default!;
    private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    protected EntityQuery<TemperatureComponent> TemperatureQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        TemperatureQuery = GetEntityQuery<TemperatureComponent>();

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PhysicsComponent, HeatCapacityUpdateEvent>(OnHeatCapacityUpdate);
        SubscribeLocalEvent<TemperatureComponent, MassDataChangedEvent>(OnMassChanged);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<TemperatureProtectionComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(OnTemperatureChangeAttempt);

        // ViewVariables
        var vvHandle = Vvm.GetTypeHandler<TemperatureComponent>();
        vvHandle.AddPath(nameof(TemperatureComponent.CurrentTemperature), (_, comp) => comp.CurrentTemperature, (uid, value, comp) => SetTemperature((uid, comp), value));
        vvHandle.AddPath(nameof(TemperatureComponent.BaseHeatCapacity), (_, comp) => comp.BaseHeatCapacity, (uid, value, comp) => SetHeatCapacity((uid, comp), value));
        vvHandle.AddPath(nameof(TemperatureComponent.SpecificHeat), (_, comp) => comp.SpecificHeat, (uid, value, comp) => SetSpecificHeat((uid, comp), value));
        vvHandle.AddPath(nameof(TemperatureComponent.CachedHeatCapacity), (uid, comp) => GetHeatCapacity((uid, comp)));
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<TemperatureComponent>();
        vvHandle.RemovePath(nameof(TemperatureComponent.CurrentTemperature));
        vvHandle.RemovePath(nameof(TemperatureComponent.BaseHeatCapacity));
        vvHandle.RemovePath(nameof(TemperatureComponent.SpecificHeat));
        vvHandle.RemovePath(nameof(TemperatureComponent.CachedHeatCapacity));

        base.Shutdown();
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

        if (!TemperatureQuery.Resolve(uid, ref temperature))
            return Atmospherics.MinimumHeatCapacity;

        if (temperature.HeatCapacityDirty)
            UpdateCachedHeatCapacity((uid, temperature));

        return temperature.CachedHeatCapacity;
    }

    /// <summary>
    /// Recalculates the heat capacity of an entity as necessary.
    /// </summary>
    /// <param name="entity">The entity that needs to have its heat capacity updated.</param>
    private void UpdateCachedHeatCapacity(Entity<TemperatureComponent> entity)
    {
        var (uid, temperature) = entity;

        DebugTools.Assert(temperature.HeatCapacityDirty, $"Recalculated the heat capacity of {ToPrettyString(entity)} without it having been dirtied.");
        temperature.HeatCapacityDirty = false;

        var ev = new HeatCapacityUpdateEvent(entity, temperature.BaseHeatCapacity);
        RaiseLocalEvent(uid, ref ev);
        temperature.CachedHeatCapacity = ev.HeatCapacity;

        DebugTools.Assert(temperature.HeatCapacityDirty, $"The heat capacity of {ToPrettyString(entity)} was dirtied while it was being recalculated.");
    }

    /// <summary>
    /// Dirties the heat capacity of an entity.
    /// </summary>
    /// <param name="entity">The entity to dirty the heat capacity of.</param>
    public void DirtyHeatCapacity(Entity<TemperatureComponent?> entity)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.HeatCapacityDirty = true;
    }

    /// <summary>
    /// Sets the base heat capacity of an entity.
    /// </summary>
    /// <remarks>
    /// Does not affect the heat capacity contributed by the mass of the entity or other factors.
    /// </remarks>
    /// <param name="entity">The entity to change the base heat capacity of.</param>
    /// <param name="value">The new base heat capacity for the entity.</param>
    public void SetHeatCapacity(Entity<TemperatureComponent?> entity, float value)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
            return;

        if (value == entity.Comp.BaseHeatCapacity)
            return;

        entity.Comp.BaseHeatCapacity = value;
        entity.Comp.HeatCapacityDirty = true;
    }

    /// <summary>
    /// Sets the specific heat of an entity.
    /// </summary>
    /// <remarks>
    /// Does not affect the base heat capacity of the entity or any heat capacity contributed by factors other than physics mass.
    /// </remarks>
    /// <param name="entity">The entity to change the specific heat of.</param>
    /// <param name="value">The new specific heat for the entity.</param>
    public void SetSpecificHeat(Entity<TemperatureComponent?> entity, float value)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
            return;

        if (value == entity.Comp.SpecificHeat)
            return;

        entity.Comp.SpecificHeat = value;
        entity.Comp.HeatCapacityDirty = true;
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
    /// Allows the physics mass of an entity to contribute heat capacity to it.
    /// </summary>
    private void OnHeatCapacityUpdate(Entity<PhysicsComponent> entity, ref HeatCapacityUpdateEvent args)
    {
        if (args.Entity.Comp.SpecificHeat == 0f || entity.Comp.FixturesMass == 0f)
            return;

        args.HeatCapacity += args.Entity.Comp.SpecificHeat * entity.Comp.FixturesMass;
    }

    /// <summary>
    /// Dirties the heat capacity of entities with mass-dependent heat capacity when their mass changes.
    /// </summary>
    private void OnMassChanged(Entity<TemperatureComponent> entity, ref MassDataChangedEvent args)
    {
        if (!args.MassChanged || entity.Comp.SpecificHeat == 0f)
            return;

        entity.Comp.HeatCapacityDirty = true;
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

/// <summary>
/// Event raised whenever the temperature of an entity changes.
/// </summary>
[ByRefEvent]
public readonly record struct OnTemperatureChangeEvent(float CurrentTemperature, float LastTemperature, Entity<TemperatureComponent> Entity)
{
    /// <summary>
    /// The difference between the new temperature and the previous temperature.
    /// </summary>
    public readonly float TemperatureDelta = CurrentTemperature - LastTemperature;
}

/// <summary>
/// <para>Event raised whenever the heat capacity of an entity is recalculated.</para>
/// <para>Used to accumulate heat capacity from sources such as fixtures and material composition.</para>
/// </summary>
[ByRefEvent]
public record struct HeatCapacityUpdateEvent(Entity<TemperatureComponent> Entity, float HeatCapacity = 0f)
{
    public readonly Entity<TemperatureComponent> Entity = Entity;
}
