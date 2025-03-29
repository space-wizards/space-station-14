using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

using Content.Shared.Projectiles;

namespace Content.Shared.Temperature.Systems;

public abstract partial class SharedTemperatureSystem : EntitySystem
{
    [Dependency] protected readonly AlertsSystem Alerts = default!;
    [Dependency] protected readonly SharedAtmosphereSystem Atmosphere = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;

    /// <summary>
    ///     All the components that will have their damage updated at the end of the tick.
    ///     This is done because both AtmosExposed and Flammable call ChangeHeat in the same tick, meaning
    ///     that we need some mechanism to ensure it doesn't double-dip on damage for both calls.
    /// </summary>
    public HashSet<Entity<TemperatureComponent>> ShouldUpdateDamage = new();

    public float UpdateInterval = 1.0f;

    private float _accumulatedFrametime;

    private EntityQuery<TemperatureComponent> _temperatureQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<ContainerTemperatureDamageThresholdsComponent> _thresholdsQuery;

    public static readonly ProtoId<AlertCategoryPrototype> TemperatureAlertCategory = "Temperature";

    public override void Initialize()
    {
        _temperatureQuery = GetEntityQuery<TemperatureComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _thresholdsQuery = GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>();

        SubscribeLocalEvent<TemperatureComponent, OnTemperatureChangeEvent>(EnqueueDamage);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<AlertsComponent, OnTemperatureChangeEvent>(ServerAlert);
        SubscribeLocalEvent<TemperatureProtectionComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(OnTemperatureChangeAttempt);

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnInit);

        SubscribeLocalEvent<ChangeTemperatureOnCollideComponent, ProjectileHitEvent>(ChangeTemperatureOnCollide);

        // Allows overriding thresholds based on the parent's thresholds.
        SubscribeLocalEvent<TemperatureComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentStartup>(OnParentThresholdStartup);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentShutdown>(OnParentThresholdShutdown);
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
            var degrees = joules / GetHeatCapacity((uid, temp, null));
            if (temp.CurrentTemperature < comp.Temperature)
                degrees *= -1;

            // exchange heat between inside and surface
            comp.Temperature += degrees;
            Dirty(uid,comp);
            ForceChangeTemperature((uid, temp), temp.CurrentTemperature - degrees);
        }

        UpdateDamage(frameTime);
    }

    private void UpdateDamage(float frameTime)
    {
        _accumulatedFrametime += frameTime;

        if (_accumulatedFrametime < UpdateInterval)
            return;

        _accumulatedFrametime -= UpdateInterval;

        if (ShouldUpdateDamage.Count == 0)
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

    public void ForceChangeTemperature(Entity<TemperatureComponent?> temperature, float temp)
    {
        if (!Resolve(temperature, ref temperature.Comp))
            return;

        var lastTemp = temperature.Comp.CurrentTemperature;
        var delta = temperature.Comp.CurrentTemperature - temp;
        if (delta == 0) //we don't want to dirty or raise events if there was no actual temp change!
            return;
        temperature.Comp.CurrentTemperature = temp;
        Dirty(temperature);

        var ev = new OnTemperatureChangeEvent(temperature.Comp.CurrentTemperature, lastTemp, delta);
        RaiseLocalEvent(temperature, ev, true);
    }

    public void ChangeHeat(Entity<TemperatureComponent?> target, float heatAmount, bool ignoreHeatResistance = false)
    {
        if (!Resolve(target, ref target.Comp, false))
            return;

        if (!ignoreHeatResistance)
        {
            var modifyChangedTempEvent = new ModifyChangedTemperatureEvent(heatAmount);
            RaiseLocalEvent(target, modifyChangedTempEvent);
            heatAmount = modifyChangedTempEvent.TemperatureDelta;
        }

        var lastTemp = target.Comp.CurrentTemperature;
        var delta = heatAmount / GetHeatCapacity((target, target, null));
        target.Comp.CurrentTemperature += delta;
        Dirty(target);

        var onTempChangeEvent = new OnTemperatureChangeEvent(target.Comp.CurrentTemperature, lastTemp, delta);
        RaiseLocalEvent(target, onTempChangeEvent, true);
    }

    public float GetHeatCapacity(Entity<TemperatureComponent?, PhysicsComponent?> target)
    {
        if (!Resolve(target, ref target.Comp1)
            || !Resolve(target, ref target.Comp2, false)
            || target.Comp2.FixturesMass <= 0)
        {
            return Atmospherics.MinimumHeatCapacity;
        }

        return target.Comp1.SpecificHeat * target.Comp2.FixturesMass;
    }

    private void OnInit(EntityUid uid, InternalTemperatureComponent comp, MapInitEvent args)
    {
        if (!TryComp<TemperatureComponent>(uid, out var temp))
            return;

        comp.Temperature = temp.CurrentTemperature;
        Dirty(uid, comp);
    }

    private void OnRejuvenate(EntityUid uid, TemperatureComponent comp, RejuvenateEvent args)
    {
        //Set internal temperature to normal body temp if there is a thermal regulator.
        //Just in case the species has a higher than normal healthy body temp (plasmamen say hi :P)
        var temperature = TryComp(uid, out ThermalRegulatorComponent? thermalReg)
            ? thermalReg.NormalBodyTemperature
            : Atmospherics.T20C;
        ForceChangeTemperature((uid, comp), temperature);
    }

    private void ServerAlert(EntityUid uid, AlertsComponent status, OnTemperatureChangeEvent args)
    {
        ProtoId<AlertPrototype> type;
        float threshold;
        float idealTemp;

        if (!TryComp<TemperatureComponent>(uid, out var temperature))
        {
            Alerts.ClearAlertCategory(uid, TemperatureAlertCategory);
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
                Alerts.ShowAlert(uid, type, 3);
                break;

            case <= 0.4f:
                Alerts.ShowAlert(uid, type, 2);
                break;

            case <= 0.66f:
                Alerts.ShowAlert(uid, type, 1);
                break;

            case > 0.66f:
                Alerts.ClearAlertCategory(uid, TemperatureAlertCategory);
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
                AdminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking high temperature damage");
                temperature.TakingDamage = true;
                Dirty(uid, temperature);
            }

            var diff = Math.Abs(temperature.CurrentTemperature - heatDamageThreshold);
            var tempDamage = c / (1 + a * Math.Pow(Math.E, -heatK * diff)) - y;
            Damageable.TryChangeDamage(uid, temperature.HeatDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.CurrentTemperature <= coldDamageThreshold)
        {
            if (!temperature.TakingDamage)
            {
                AdminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking low temperature damage");
                temperature.TakingDamage = true;
                Dirty(uid, temperature);
            }

            var diff = Math.Abs(temperature.CurrentTemperature - coldDamageThreshold);
            var tempDamage = Math.Sqrt(diff * (Math.Pow(temperature.DamageCap.Double(), 2) / coldDamageThreshold));
            Damageable.TryChangeDamage(uid, temperature.ColdDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.TakingDamage)
        {
            AdminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} stopped taking temperature damage");
            temperature.TakingDamage = false;
            Dirty(uid, temperature);
        }
    }

    private void OnTemperatureChangeAttempt(
        EntityUid uid,
        TemperatureProtectionComponent component,
        InventoryRelayedEvent<ModifyChangedTemperatureEvent> args
    )
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
        ChangeHeat((args.Target, null), ent.Comp.Heat, ent.Comp.IgnoreHeatResistance); // adjust the temperature
    }

    private void OnParentChange(EntityUid uid, TemperatureComponent component, ref EntParentChangedMessage args)
    {
        // We only need to update thresholds if the thresholds changed for the entity's ancestors.
        var oldThresholds = args.OldParent != null
            ? RecalculateParentThresholds(args.OldParent.Value)
            : (null, null);
        var newThresholds = RecalculateParentThresholds(_transformQuery.GetComponent(uid).ParentUid);

        if (oldThresholds == newThresholds)
            return;

        RecursiveThresholdUpdate(uid);
    }

    private void OnParentThresholdStartup(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component, ComponentStartup args)
    {
        RecursiveThresholdUpdate(uid);
    }

    private void OnParentThresholdShutdown(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component, ComponentShutdown args)
    {
        RecursiveThresholdUpdate(uid);
    }

    /// <summary>
    /// Recalculate and apply parent thresholds for the root entity and all its descendant.
    /// </summary>
    private void RecursiveThresholdUpdate(EntityUid root)
    {
        RecalculateAndApplyParentThresholds(root);

        var enumerator = Transform(root).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            RecursiveThresholdUpdate(child);
        }
    }

    /// <summary>
    /// Recalculate parent thresholds and apply them on the uid temperature component.
    /// </summary>
    private void RecalculateAndApplyParentThresholds(EntityUid uid)
    {
        if (!_temperatureQuery.TryGetComponent(uid, out var temperature))
        {
            return;
        }

        var parentUid = _transformQuery.GetComponent(uid).ParentUid;
        var newThresholds = RecalculateParentThresholds(parentUid);
        temperature.ParentHeatDamageThreshold = newThresholds.HeatThreshold;
        temperature.ParentColdDamageThreshold = newThresholds.ColdThreshold;
        Dirty(uid, temperature);
    }

    /// <summary>
    /// Recalculate Parent Heat/Cold DamageThreshold by recursively checking each ancestor and fetching the
    /// maximum HeatDamageThreshold and the minimum ColdDamageThreshold if any exists (aka the best value for each).
    /// Will return null for each new threshold value not found.
    /// </summary>
    private (float? HeatThreshold, float? ColdThreshold) RecalculateParentThresholds(EntityUid initialParentUid)
    {
        // Recursively check parents for the best threshold available
        var parentUid = initialParentUid;
        float? newHeatThreshold = null;
        float? newColdThreshold = null;
        while (parentUid.IsValid())
        {
            if (_thresholdsQuery.TryGetComponent(parentUid, out var newThresholds))
            {
                if (newThresholds.HeatDamageThreshold != null)
                {
                    newHeatThreshold = Math.Max(newThresholds.HeatDamageThreshold.Value, newHeatThreshold ?? 0);
                }

                if (newThresholds.ColdDamageThreshold != null)
                {
                    newColdThreshold = Math.Min(newThresholds.ColdDamageThreshold.Value, newColdThreshold ?? float.MaxValue);
                }
            }

            parentUid = _transformQuery.GetComponent(parentUid).ParentUid;
        }

        return (newHeatThreshold, newColdThreshold);
    }

    [Obsolete("Use Entity<T> overload instead!")]
    public void ForceChangeTemperature(EntityUid target, float temp, TemperatureComponent? temperature = null)
    {
        ForceChangeTemperature((target, temperature), temp);
    }

    [Obsolete("Use Entity<T> overload instead!")]
    public float GetHeatCapacity(EntityUid uid, TemperatureComponent? comp = null, PhysicsComponent? physics = null)
    {
        return GetHeatCapacity((uid, comp, physics));
    }

    [Obsolete("Use Entity<T> overload instead!")]
    public void ChangeHeat(EntityUid uid,
        float heatAmount,
        bool ignoreHeatResistance = false,
        TemperatureComponent? temperature = null)
    {
        ChangeHeat((uid, temperature), heatAmount, ignoreHeatResistance);
    }
}
