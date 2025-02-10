using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Temperature.Systems;

public sealed class TemperatureSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IViewVariablesManager _vvManager = default!;

    /// <summary>
    ///     All the components that will have their damage updated at the end of the tick.
    ///     This is done because both AtmosExposed and Flammable call ChangeHeat in the same tick, meaning
    ///     that we need some mechanism to ensure it doesn't double dip on damage for both calls.
    /// </summary>
    public HashSet<Entity<TemperatureComponent>> ShouldUpdateDamage = new();

    public float UpdateInterval = 1.0f;

    private float _accumulatedFrametime;

    [ValidatePrototypeId<AlertCategoryPrototype>]
    public const string TemperatureAlertCategory = "Temperature";

    public override void Initialize()
    {
        SubscribeLocalEvent<TemperatureComponent, OnTemperatureChangeEvent>(EnqueueDamage);
        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<AlertsComponent, OnTemperatureChangeEvent>(ServerAlert);
        SubscribeLocalEvent<TemperatureProtectionComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(
            OnTemperatureChangeAttempt);

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnInit);

        SubscribeLocalEvent<TemperatureComponent, MassDataChangedEvent>(OnMassDataChanged);
        SubscribeLocalEvent<PhysicsComponent, RecalculateHeatCapacityEvent>(OnRecalculateHeatCapacity);

        SubscribeLocalEvent<ChangeTemperatureOnCollideComponent, ProjectileHitEvent>(ChangeTemperatureOnCollide);

        // Allows overriding thresholds based on the parent's thresholds.
        SubscribeLocalEvent<TemperatureComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentStartup>(
            OnParentThresholdStartup);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentShutdown>(
            OnParentThresholdShutdown);

        var vvHandle = _vvManager.GetTypeHandler<TemperatureComponent>();
        vvHandle.AddPath(nameof(TemperatureComponent.TotalHeatCapacity), (uid, comp) => GetHeatCapacity(uid, comp));
        vvHandle.AddPath(nameof(TemperatureComponent.BaseHeatCapacity), (_, comp) => comp.BaseHeatCapacity, (uid, value, comp) => SetBaseHeatCapacity((uid, comp), value));
        vvHandle.AddPath(nameof(TemperatureComponent.SpecificHeat), (_, comp) => comp.SpecificHeat, (uid, value, comp) => SetSpecificHeat((uid, comp), value));
    }

    public override void Shutdown()
    {
        var vvHandle = _vvManager.GetTypeHandler<TemperatureComponent>();
        vvHandle.RemovePath(nameof(TemperatureComponent.TotalHeatCapacity));
        vvHandle.RemovePath(nameof(TemperatureComponent.BaseHeatCapacity));
        vvHandle.RemovePath(nameof(TemperatureComponent.SpecificHeat));

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
            var degrees = joules / GetHeatCapacity(uid, temp);
            if (temp.CurrentTemperature < comp.Temperature)
                degrees *= -1;

            // exchange heat between inside and surface
            comp.Temperature += degrees;
            ForceChangeTemperature(uid, temp.CurrentTemperature - degrees, temp);
        }

        UpdateDamage(frameTime);
    }

    private void UpdateDamage(float frameTime)
    {
        _accumulatedFrametime += frameTime;

        if (_accumulatedFrametime < UpdateInterval)
            return;
        _accumulatedFrametime -= UpdateInterval;

        if (!ShouldUpdateDamage.Any())
            return;

        foreach (var comp in ShouldUpdateDamage)
        {
            MetaDataComponent? metaData = null;

            var uid = comp.Owner;
            if (Deleted(uid, metaData) || Paused(uid, metaData))
                continue;

            ChangeDamage(uid, comp);
        }

        ShouldUpdateDamage.Clear();
    }

    public void ForceChangeTemperature(EntityUid uid, float temp, TemperatureComponent? temperature = null)
    {
        if (!Resolve(uid, ref temperature))
            return;

        float lastTemp = temperature.CurrentTemperature;
        float delta = temperature.CurrentTemperature - temp;
        temperature.CurrentTemperature = temp;
        RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta),
            true);
    }

    public void ChangeHeat(EntityUid uid, float heatAmount, bool ignoreHeatResistance = false,
        TemperatureComponent? temperature = null)
    {
        if (!Resolve(uid, ref temperature, false))
            return;

        if (!ignoreHeatResistance)
        {
            var ev = new ModifyChangedTemperatureEvent(heatAmount);
            RaiseLocalEvent(uid, ev);
            heatAmount = ev.TemperatureDelta;
        }

        float lastTemp = temperature.CurrentTemperature;
        temperature.CurrentTemperature += heatAmount / GetHeatCapacity(uid, temperature);
        float delta = temperature.CurrentTemperature - lastTemp;

        RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta), true);
    }

    private void OnAtmosExposedUpdate(EntityUid uid, TemperatureComponent temperature,
        ref AtmosExposedUpdateEvent args)
    {
        var transform = args.Transform;

        if (transform.MapUid == null)
            return;

        var temperatureDelta = args.GasMixture.Temperature - temperature.CurrentTemperature;
        var airHeatCapacity = _atmosphere.GetHeatCapacity(args.GasMixture, false);
        var heatCapacity = GetHeatCapacity(uid, temperature);
        var heat = temperatureDelta * (airHeatCapacity * heatCapacity /
                                       (airHeatCapacity + heatCapacity));
        ChangeHeat(uid, heat * temperature.AtmosTemperatureTransferEfficiency, temperature: temperature);
    }

    /// <summary>
    /// Fetches the total heat capacity for an entity.
    /// Will recalculate the value as necessary to reflect any changes to the sources of heat capacity.
    /// </summary>
    /// <param name="uid">The entity that is having its heat capacity recalculated.</param>
    /// <param name="comp">The thermal data (temperature and heat capacity) for the entity.</param>
    /// <param name="physics">[Obsolete]</param>
    /// <returns>The total heat capacity of the entity.</returns>
    public float GetHeatCapacity(EntityUid uid, TemperatureComponent? comp = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref comp))
            return Atmospherics.MinimumHeatCapacity;

        if (comp.HeatCapacityDirty)
            RecalculateHeatCapacity((uid, comp));

        return comp.TotalHeatCapacity;
    }

    /// <summary>
    /// Recalculates the total heat capacity for an entity based on all contributing sources.
    /// </summary>
    private void RecalculateHeatCapacity(Entity<TemperatureComponent> ent)
    {
        DebugTools.Assert(ent.Comp.HeatCapacityDirty, $"The heat capacity for {ToPrettyString(ent)} was recalculated without being dirtied.");
        ent.Comp.HeatCapacityDirty = false;

        var ev = new RecalculateHeatCapacityEvent(ent, ent.Comp.BaseHeatCapacity);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.TotalHeatCapacity = ev.HeatCapacity;

        DebugTools.Assert(!ent.Comp.HeatCapacityDirty, $"The heat capacity for {ToPrettyString(ent)} was dirtied while it was being recalculated.");
    }

    /// <summary>
    /// Adjusts the total heat capacity of an entity by some amount.
    /// May dirty the total heat capacity of the entity if called enough times between recalculations.
    /// </summary>
    /// <remarks>
    /// Should only be used in response to changes to a source of heat capacity for the entity.
    /// </remarks>
    /// <param name="ent">The entity that will have its total heat capacity modified.</param>
    /// <param name="delta">The amount by which to increase or decrease the entities heat capacity.</param>
    public void AdjustTotalHeatCapacity(Entity<TemperatureComponent?> ent, float delta)
    {
        if (!Resolve(ent, ref ent.Comp) || delta == 0f)
            return;

        ent.Comp.TotalHeatCapacity += delta;

        if (++ent.Comp.HeatCapacityTouched >= TemperatureComponent.HeatCapacityUpdateInterval)
            ent.Comp.HeatCapacityDirty = true;
    }

    /// <summary>
    /// Marks the total heat capacity of an entity as out of date.
    /// </summary>
    /// <remarks>
    /// Should be used every time a source of heat capacity for the entity changes.
    /// Prefer <see cref="AdjustTotalHeatCapacity"/> if you know the amount by which the heat capacity has changed.
    /// </remarks>
    public void DirtyHeatCapacity(Entity<TemperatureComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.HeatCapacityDirty = true;
    }

    /// <summary>
    /// Sets the base heat capacity of an entity.
    /// </summary>
    /// <param name="ent">The entity to modify the base heat capacity of.</param>
    /// <param name="value">The new base heat capacity for the entity.</param>
    public void SetBaseHeatCapacity(Entity<TemperatureComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var oldHeatCapacity = ent.Comp.BaseHeatCapacity;
        if (value == oldHeatCapacity)
            return;

        ent.Comp.BaseHeatCapacity = value;
        AdjustTotalHeatCapacity(ent, value - oldHeatCapacity);
    }

    /// <summary>
    /// Sets how much additional heat capacity an entity gets from each kg of mass it has.
    /// </summary>
    /// <param name="ent">The entity to modify the specific heat of.</param>
    /// <param name="value">The new specific heat for the entity.</param>
    public void SetSpecificHeat(Entity<TemperatureComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (value == ent.Comp.SpecificHeat)
            return;

        ent.Comp.SpecificHeat = value;
        DirtyHeatCapacity(ent); // Assume the entity has PhysicsComponent and nonzero mass.
    }

    private void OnInit(EntityUid uid, InternalTemperatureComponent comp, MapInitEvent args)
    {
        if (!TryComp<TemperatureComponent>(uid, out var temp))
            return;

        comp.Temperature = temp.CurrentTemperature;
    }

    /// <summary>
    /// Marks the heat capacity of the entity as having changed when the mass of the entity changes if its heat capacity is dependent on that.
    /// </summary>
    private void OnMassDataChanged(Entity<TemperatureComponent> ent, ref MassDataChangedEvent args)
    {
        if (!args.MassChanged || ent.Comp.SpecificHeat == 0f)
            return;

        AdjustTotalHeatCapacity((ent, ent.Comp), ent.Comp.SpecificHeat * (args.NewMass - args.OldMass));
    }

    /// <summary>
    /// Accumulates additional heat capacity for the entity based on its specific heat and mass.
    /// </summary>
    private void OnRecalculateHeatCapacity(Entity<PhysicsComponent> ent, ref RecalculateHeatCapacityEvent args)
    {
        if (args.Entity.Comp.SpecificHeat == 0f || ent.Comp.FixturesMass == 0f)
            return;

        args.HeatCapacity += args.Entity.Comp.SpecificHeat * ent.Comp.FixturesMass;
    }

    private void OnRejuvenate(EntityUid uid, TemperatureComponent comp, RejuvenateEvent args)
    {
        ForceChangeTemperature(uid, Atmospherics.T20C, comp);
    }

    private void ServerAlert(EntityUid uid, AlertsComponent status, OnTemperatureChangeEvent args)
    {
        ProtoId<AlertPrototype> type;
        float threshold;
        float idealTemp;

        if (!TryComp<TemperatureComponent>(uid, out var temperature))
        {
            _alerts.ClearAlertCategory(uid, TemperatureAlertCategory);
            return;
        }

        if (TryComp<ThermalRegulatorComponent>(uid, out var regulator) &&
            regulator.NormalBodyTemperature > temperature.ColdDamageThreshold &&
            regulator.NormalBodyTemperature < temperature.HeatDamageThreshold)
        {
            idealTemp = regulator.NormalBodyTemperature;
        }
        else
        {
            idealTemp = (temperature.ColdDamageThreshold + temperature.HeatDamageThreshold) / 2;
        }

        if (args.CurrentTemperature <= idealTemp)
        {
            type = temperature.ColdAlert;
            threshold = temperature.ColdDamageThreshold;
        }
        else
        {
            type = temperature.HotAlert;
            threshold = temperature.HeatDamageThreshold;
        }

        // Calculates a scale where 1.0 is the ideal temperature and 0.0 is where temperature damage begins
        // The cold and hot scales will differ in their range if the ideal temperature is not exactly halfway between the thresholds
        var tempScale = (args.CurrentTemperature - threshold) / (idealTemp - threshold);
        switch (tempScale)
        {
            case <= 0f:
                _alerts.ShowAlert(uid, type, 3);
                break;

            case <= 0.4f:
                _alerts.ShowAlert(uid, type, 2);
                break;

            case <= 0.66f:
                _alerts.ShowAlert(uid, type, 1);
                break;

            case > 0.66f:
                _alerts.ClearAlertCategory(uid, TemperatureAlertCategory);
                break;
        }
    }

    private void EnqueueDamage(Entity<TemperatureComponent> temperature, ref OnTemperatureChangeEvent args)
    {
        ShouldUpdateDamage.Add(temperature);
    }

    private void ChangeDamage(EntityUid uid, TemperatureComponent temperature)
    {
        if (!HasComp<DamageableComponent>(uid))
            return;

        // See this link for where the scaling func comes from:
        // https://www.desmos.com/calculator/0vknqtdvq9
        // Based on a logistic curve, which caps out at MaxDamage
        var heatK = 0.005;
        var a = 1;
        var y = temperature.DamageCap;
        var c = y * 2;

        var heatDamageThreshold = temperature.ParentHeatDamageThreshold ?? temperature.HeatDamageThreshold;
        var coldDamageThreshold = temperature.ParentColdDamageThreshold ?? temperature.ColdDamageThreshold;

        if (temperature.CurrentTemperature >= heatDamageThreshold)
        {
            if (!temperature.TakingDamage)
            {
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking high temperature damage");
                temperature.TakingDamage = true;
            }

            var diff = Math.Abs(temperature.CurrentTemperature - heatDamageThreshold);
            var tempDamage = c / (1 + a * Math.Pow(Math.E, -heatK * diff)) - y;
            _damageable.TryChangeDamage(uid, temperature.HeatDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.CurrentTemperature <= coldDamageThreshold)
        {
            if (!temperature.TakingDamage)
            {
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking low temperature damage");
                temperature.TakingDamage = true;
            }

            var diff = Math.Abs(temperature.CurrentTemperature - coldDamageThreshold);
            var tempDamage =
                Math.Sqrt(diff * (Math.Pow(temperature.DamageCap.Double(), 2) / coldDamageThreshold));
            _damageable.TryChangeDamage(uid, temperature.ColdDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.TakingDamage)
        {
            _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} stopped taking temperature damage");
            temperature.TakingDamage = false;
        }
    }

    private void OnTemperatureChangeAttempt(EntityUid uid, TemperatureProtectionComponent component,
        InventoryRelayedEvent<ModifyChangedTemperatureEvent> args)
    {
        var coefficient = args.Args.TemperatureDelta < 0
            ? component.CoolingCoefficient
            : component.HeatingCoefficient;

        var ev = new GetTemperatureProtectionEvent(coefficient);
        RaiseLocalEvent(uid, ref ev);

        args.Args.TemperatureDelta *= ev.Coefficient;
    }

    private void ChangeTemperatureOnCollide(Entity<ChangeTemperatureOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ChangeHeat(args.Target, ent.Comp.Heat, ent.Comp.IgnoreHeatResistance);// adjust the temperature
    }

    private void OnParentChange(EntityUid uid, TemperatureComponent component,
        ref EntParentChangedMessage args)
    {
        var temperatureQuery = GetEntityQuery<TemperatureComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();
        var thresholdsQuery = GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>();
        // We only need to update thresholds if the thresholds changed for the entity's ancestors.
        var oldThresholds = args.OldParent != null
            ? RecalculateParentThresholds(args.OldParent.Value, transformQuery, thresholdsQuery)
            : (null, null);
        var newThresholds = RecalculateParentThresholds(transformQuery.GetComponent(uid).ParentUid, transformQuery, thresholdsQuery);

        if (oldThresholds != newThresholds)
        {
            RecursiveThresholdUpdate(uid, temperatureQuery, transformQuery, thresholdsQuery);
        }
    }

    private void OnParentThresholdStartup(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component,
        ComponentStartup args)
    {
        RecursiveThresholdUpdate(uid, GetEntityQuery<TemperatureComponent>(), GetEntityQuery<TransformComponent>(),
            GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>());
    }

    private void OnParentThresholdShutdown(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component,
        ComponentShutdown args)
    {
        RecursiveThresholdUpdate(uid, GetEntityQuery<TemperatureComponent>(), GetEntityQuery<TransformComponent>(),
            GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>());
    }

    /// <summary>
    /// Recalculate and apply parent thresholds for the root entity and all its descendant.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="temperatureQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private void RecursiveThresholdUpdate(EntityUid root, EntityQuery<TemperatureComponent> temperatureQuery,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> tempThresholdsQuery)
    {
        RecalculateAndApplyParentThresholds(root, temperatureQuery, transformQuery, tempThresholdsQuery);

        var enumerator = Transform(root).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            RecursiveThresholdUpdate(child, temperatureQuery, transformQuery, tempThresholdsQuery);
        }
    }

    /// <summary>
    /// Recalculate parent thresholds and apply them on the uid temperature component.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="temperatureQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private void RecalculateAndApplyParentThresholds(EntityUid uid,
        EntityQuery<TemperatureComponent> temperatureQuery, EntityQuery<TransformComponent> transformQuery,
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> tempThresholdsQuery)
    {
        if (!temperatureQuery.TryGetComponent(uid, out var temperature))
        {
            return;
        }

        var newThresholds = RecalculateParentThresholds(transformQuery.GetComponent(uid).ParentUid, transformQuery, tempThresholdsQuery);
        temperature.ParentHeatDamageThreshold = newThresholds.Item1;
        temperature.ParentColdDamageThreshold = newThresholds.Item2;
    }

    /// <summary>
    /// Recalculate Parent Heat/Cold DamageThreshold by recursively checking each ancestor and fetching the
    /// maximum HeatDamageThreshold and the minimum ColdDamageThreshold if any exists (aka the best value for each).
    /// </summary>
    /// <param name="initialParentUid"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private (float?, float?) RecalculateParentThresholds(
        EntityUid initialParentUid,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> tempThresholdsQuery)
    {
        // Recursively check parents for the best threshold available
        var parentUid = initialParentUid;
        float? newHeatThreshold = null;
        float? newColdThreshold = null;
        while (parentUid.IsValid())
        {
            if (tempThresholdsQuery.TryGetComponent(parentUid, out var newThresholds))
            {
                if (newThresholds.HeatDamageThreshold != null)
                {
                    newHeatThreshold = Math.Max(newThresholds.HeatDamageThreshold.Value,
                        newHeatThreshold ?? 0);
                }

                if (newThresholds.ColdDamageThreshold != null)
                {
                    newColdThreshold = Math.Min(newThresholds.ColdDamageThreshold.Value,
                        newColdThreshold ?? float.MaxValue);
                }
            }

            parentUid = transformQuery.GetComponent(parentUid).ParentUid;
        }

        return (newHeatThreshold, newColdThreshold);
    }
}

/// <summary>
/// A directed event raised when the total heat capacity of an entity is recalculated.
/// </summary>
/// <param name="Entity">The entity that is having its heat capacity recalculated.</param>
/// <param name="HeatCapacity">An accumulator used to aggregate heat capacity from sources.</param>
[ByRefEvent]
public record struct RecalculateHeatCapacityEvent(Entity<TemperatureComponent> Entity, float HeatCapacity = 0f)
{
    public readonly Entity<TemperatureComponent> Entity = Entity;
}
