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
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Systems;

public sealed class TemperatureSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    /// <summary>
    ///     All the components that will have their damage updated at the end of the tick.
    ///     This is done because both AtmosExposed and Flammable call ChangeHeat in the same tick, meaning
    ///     that we need some mechanism to ensure it doesn't double dip on damage for both calls.
    /// </summary>
    public HashSet<Entity<TemperatureDamageThresholdsComponent>> ShouldUpdateDamage = new();

    public float UpdateInterval = 1.0f;

    private float _accumulatedFrametime;

    [ValidatePrototypeId<AlertCategoryPrototype>]
    public const string TemperatureAlertCategory = "Temperature";

    public override void Initialize()
    {
        SubscribeLocalEvent<TemperatureDamageThresholdsComponent, OnTemperatureChangeEvent>(EnqueueDamage);
        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<AlertsComponent, OnTemperatureChangeEvent>(ServerAlert);
        SubscribeLocalEvent<TemperatureProtectionComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(
            OnTemperatureChangeAttempt);

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnInit);

        // Allows overriding thresholds based on the parent's thresholds.
        SubscribeLocalEvent<TemperatureDamageThresholdsComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentStartup>(
            OnParentThresholdStartup);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentShutdown>(
            OnParentThresholdShutdown);
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
        if (!Resolve(uid, ref temperature))
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

    public float GetHeatCapacity(EntityUid uid, TemperatureComponent? comp = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref comp) || !Resolve(uid, ref physics, false) || physics.FixturesMass <= 0)
        {
            return Atmospherics.MinimumHeatCapacity;
        }

        return comp.SpecificHeat * physics.FixturesMass;
    }

    private void OnInit(EntityUid uid, InternalTemperatureComponent comp, MapInitEvent args)
    {
        if (!TryComp<TemperatureComponent>(uid, out var temp))
            return;

        comp.Temperature = temp.CurrentTemperature;
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

        if (!TryComp<TemperatureDamageThresholdsComponent>(uid, out var thresholds))
        {
            _alerts.ClearAlertCategory(uid, TemperatureAlertCategory);
            return;
        }

        if (TryComp<ThermalRegulatorComponent>(uid, out var regulator) &&
            regulator.NormalBodyTemperature > thresholds.ColdDamageThreshold &&
            regulator.NormalBodyTemperature < thresholds.HeatDamageThreshold)
        {
            idealTemp = regulator.NormalBodyTemperature;
        }
        else
        {
            idealTemp = (thresholds.ColdDamageThreshold + thresholds.HeatDamageThreshold) / 2;
        }

        if (args.CurrentTemperature <= idealTemp)
        {
            type = thresholds.ColdAlert;
            threshold = thresholds.ColdDamageThreshold;
        }
        else
        {
            type = thresholds.HotAlert;
            threshold = thresholds.HeatDamageThreshold;
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

    private void EnqueueDamage(Entity<TemperatureDamageThresholdsComponent> ent, ref OnTemperatureChangeEvent args)
    {
        ShouldUpdateDamage.Add(ent);
    }

    private void ChangeDamage(EntityUid uid, TemperatureDamageThresholdsComponent thresholds)
    {
        if (!HasComp<DamageableComponent>(uid) || !TryComp<TemperatureComponent>(uid, out var temperature))
            return;

        // See this link for where the scaling func comes from:
        // https://www.desmos.com/calculator/0vknqtdvq9
        // Based on a logistic curve, which caps out at MaxDamage
        var heatK = 0.005;
        var a = 1;
        var y = thresholds.DamageCap;
        var c = y * 2;

        var heatDamageThreshold = thresholds.ParentHeatDamageThreshold ?? thresholds.HeatDamageThreshold;
        var coldDamageThreshold = thresholds.ParentColdDamageThreshold ?? thresholds.ColdDamageThreshold;

        if (temperature.CurrentTemperature >= heatDamageThreshold)
        {
            if (!thresholds.TakingDamage)
            {
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking high temperature damage");
                thresholds.TakingDamage = true;
            }

            var diff = Math.Abs(temperature.CurrentTemperature - heatDamageThreshold);
            var tempDamage = c / (1 + a * Math.Pow(Math.E, -heatK * diff)) - y;
            _damageable.TryChangeDamage(uid, thresholds.HeatDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.CurrentTemperature <= coldDamageThreshold)
        {
            if (!thresholds.TakingDamage)
            {
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking low temperature damage");
                thresholds.TakingDamage = true;
            }

            var diff = Math.Abs(temperature.CurrentTemperature - coldDamageThreshold);
            var tempDamage =
                Math.Sqrt(diff * (Math.Pow(thresholds.DamageCap.Double(), 2) / coldDamageThreshold));
            _damageable.TryChangeDamage(uid, thresholds.ColdDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (thresholds.TakingDamage)
        {
            _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} stopped taking temperature damage");
            thresholds.TakingDamage = false;
        }
    }

    private void OnTemperatureChangeAttempt(EntityUid uid, TemperatureProtectionComponent component,
        InventoryRelayedEvent<ModifyChangedTemperatureEvent> args)
    {
        var ev = new GetTemperatureProtectionEvent(component.Coefficient);
        RaiseLocalEvent(uid, ref ev);

        args.Args.TemperatureDelta *= ev.Coefficient;
    }

    private void OnParentChange(EntityUid uid, TemperatureDamageThresholdsComponent component,
        ref EntParentChangedMessage args)
    {
        var thresholdsQuery = GetEntityQuery<TemperatureDamageThresholdsComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();
        var containerThresholdsQuery = GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>();
        // We only need to update thresholds if the thresholds changed for the entity's ancestors.
        var oldThresholds = args.OldParent != null
            ? RecalculateParentThresholds(args.OldParent.Value, transformQuery, containerThresholdsQuery)
            : (null, null);
        var newThresholds = RecalculateParentThresholds(transformQuery.GetComponent(uid).ParentUid, transformQuery, containerThresholdsQuery);

        if (oldThresholds != newThresholds)
        {
            RecursiveThresholdUpdate(uid, thresholdsQuery, transformQuery, containerThresholdsQuery);
        }
    }

    private void OnParentThresholdStartup(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component,
        ComponentStartup args)
    {
        RecursiveThresholdUpdate(uid, GetEntityQuery<TemperatureDamageThresholdsComponent>(), GetEntityQuery<TransformComponent>(),
            GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>());
    }

    private void OnParentThresholdShutdown(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component,
        ComponentShutdown args)
    {
        RecursiveThresholdUpdate(uid, GetEntityQuery<TemperatureDamageThresholdsComponent>(), GetEntityQuery<TransformComponent>(),
            GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>());
    }

    /// <summary>
    /// Recalculate and apply parent thresholds for the root entity and all its descendant.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="temperatureQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="containerThresholdsQuery"></param>
    private void RecursiveThresholdUpdate(EntityUid root, EntityQuery<TemperatureDamageThresholdsComponent> thresholdsQuery,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> containerThresholdsQuery)
    {
        RecalculateAndApplyParentThresholds(root, thresholdsQuery, transformQuery, containerThresholdsQuery);

        var enumerator = Transform(root).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            RecursiveThresholdUpdate(child, thresholdsQuery, transformQuery, containerThresholdsQuery);
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
        EntityQuery<TemperatureDamageThresholdsComponent> thresholdsQuery, EntityQuery<TransformComponent> transformQuery,
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> containerThresholdsQuery)
    {
        if (!thresholdsQuery.TryGetComponent(uid, out var thresholds))
        {
            return;
        }

        var newThresholds = RecalculateParentThresholds(transformQuery.GetComponent(uid).ParentUid, transformQuery, containerThresholdsQuery);
        thresholds.ParentHeatDamageThreshold = newThresholds.Item1;
        thresholds.ParentColdDamageThreshold = newThresholds.Item2;
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
        EntityQuery<ContainerTemperatureDamageThresholdsComponent> containerThresholdsQuery)
    {
        // Recursively check parents for the best threshold available
        var parentUid = initialParentUid;
        float? newHeatThreshold = null;
        float? newColdThreshold = null;
        while (parentUid.IsValid())
        {
            if (containerThresholdsQuery.TryGetComponent(parentUid, out var newThresholds))
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

public sealed class OnTemperatureChangeEvent : EntityEventArgs
{
    public float CurrentTemperature { get; }
    public float LastTemperature { get; }
    public float TemperatureDelta { get; }

    public OnTemperatureChangeEvent(float current, float last, float delta)
    {
        CurrentTemperature = current;
        LastTemperature = last;
        TemperatureDelta = delta;
    }
}
