using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Rounding;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles entities taking damage from being too hot or too cold.
/// Also handles alerts relevant to the same.
/// </summary>
public sealed partial class TemperatureSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private EntityQuery<TemperatureDamageComponent> _tempDamageQuery;
    private EntityQuery<ContainerTemperatureComponent> _containerTemperatureQuery;
    private EntityQuery<ThermalRegulatorComponent> _thermalRegulatorQuery;

    /// <summary>
    ///     All the components that will have their damage updated at the end of the tick.
    ///     This is done because both AtmosExposed and Flammable call ChangeHeat in the same tick, meaning
    ///     that we need some mechanism to ensure it doesn't double-dip on damage for both calls.
    /// </summary>
    public HashSet<Entity<TemperatureDamageComponent>> ShouldUpdateDamage = new();

    /// <summary>
    /// Alert prototype for Temperature.
    /// </summary>
    public static readonly ProtoId<AlertCategoryPrototype> TemperatureAlertCategory = "Temperature";

    /// <summary>
    /// The maximum severity applicable to temperature alerts.
    /// </summary>
    public static readonly short MaxTemperatureAlertSeverity = 3;

    /// <summary>
    /// On a scale of 0. to 1. where 0. is the ideal temperature and 1. is a temperature damage threshold this is the point where the component starts raising temperature alerts.
    /// </summary>
    public static readonly float MinAlertTemperatureScale = 0.33f;

    private void InitializeDamage()
    {
        SubscribeLocalEvent<AlertsComponent, OnTemperatureChangeEvent>(ServerAlert);

        SubscribeLocalEvent<TemperatureDamageComponent, OnTemperatureChangeEvent>(EnqueueDamage);
        SubscribeLocalEvent<TemperatureDamageComponent, EntityUnpausedEvent>(OnUnpaused);

        // Allows overriding thresholds based on the parent's thresholds.
        SubscribeLocalEvent<TemperatureDamageComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ContainerTemperatureComponent, ComponentStartup>(OnParentThresholdStartup);
        SubscribeLocalEvent<ContainerTemperatureComponent, ComponentShutdown>(OnParentThresholdShutdown);

        _tempDamageQuery = GetEntityQuery<TemperatureDamageComponent>();
        _containerTemperatureQuery = GetEntityQuery<ContainerTemperatureComponent>();
        _thermalRegulatorQuery = GetEntityQuery<ThermalRegulatorComponent>();
    }

    private void UpdateDamage()
    {
        foreach (var entity in ShouldUpdateDamage)
        {
            if (Deleted(entity) || Paused(entity))
                continue;

            var deltaTime = _gameTiming.CurTime - entity.Comp.LastUpdate;
            if (entity.Comp.TakingDamage && deltaTime < entity.Comp.UpdateInterval)
                continue;

            ChangeDamage(entity, deltaTime);
        }

        ShouldUpdateDamage.Clear();
    }

    private void ChangeDamage(Entity<TemperatureDamageComponent> entity, TimeSpan deltaTime)
    {
        entity.Comp.LastUpdate = _gameTiming.CurTime;

        if (!HasComp<DamageableComponent>(entity) || !TemperatureQuery.TryComp(entity, out var temperature))
            return;

        // See this link for where the scaling func comes from:
        // https://www.desmos.com/calculator/0vknqtdvq9
        // Based on a logistic curve, which caps out at MaxDamage
        var heatK = 0.005;
        var a = 1;
        var y = entity.Comp.DamageCap;
        var c = y * 2;

        var heatDamageThreshold = entity.Comp.ParentHeatDamageThreshold ?? entity.Comp.HeatDamageThreshold;
        var coldDamageThreshold = entity.Comp.ParentColdDamageThreshold ?? entity.Comp.ColdDamageThreshold;

        if (temperature.CurrentTemperature >= heatDamageThreshold)
        {
            if (!entity.Comp.TakingDamage)
            {
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(entity):entity} started taking high temperature damage");
                entity.Comp.TakingDamage = true;
            }

            var diff = Math.Abs(temperature.CurrentTemperature - heatDamageThreshold);
            var tempDamage = c / (1 + a * Math.Pow(Math.E, -heatK * diff)) - y;
            _damageable.TryChangeDamage(entity.Owner, entity.Comp.HeatDamage * tempDamage * deltaTime.TotalSeconds, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.CurrentTemperature <= coldDamageThreshold)
        {
            if (!entity.Comp.TakingDamage)
            {
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(entity):entity} started taking low temperature damage");
                entity.Comp.TakingDamage = true;
            }

            var diff = Math.Abs(temperature.CurrentTemperature - coldDamageThreshold);
            var tempDamage =
                Math.Sqrt(diff * (Math.Pow(entity.Comp.DamageCap.Double(), 2) / coldDamageThreshold));
            _damageable.TryChangeDamage(entity.Owner, entity.Comp.ColdDamage * tempDamage * deltaTime.TotalSeconds, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (entity.Comp.TakingDamage)
        {
            _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(entity):entity} stopped taking temperature damage");
            entity.Comp.TakingDamage = false;
        }
    }

    private void ServerAlert(Entity<AlertsComponent> entity, ref OnTemperatureChangeEvent args)
    {
        ProtoId<AlertPrototype> type;
        float threshold;
        float idealTemp;

        if (!_tempDamageQuery.TryComp(entity, out var thresholds))
        {
            _alerts.ClearAlertCategory(entity.Owner, TemperatureAlertCategory);
            return;
        }

        if (_thermalRegulatorQuery.TryComp(entity, out var regulator) &&
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

        // Calculates a scale where 0.0 is the ideal temperature and 1.0 is where temperature damage begins
        // The cold and hot scales will differ in their range if the ideal temperature is not exactly halfway between the thresholds
        var tempScale = (args.CurrentTemperature - idealTemp) / (threshold - idealTemp);
        var alertLevel = (short)ContentHelpers.RoundToLevels(tempScale - MinAlertTemperatureScale, 1.00f - MinAlertTemperatureScale, MaxTemperatureAlertSeverity + 1);

        if (alertLevel > 0)
            _alerts.ShowAlert(entity.AsNullable(), type, alertLevel);
        else
            _alerts.ClearAlertCategory(entity.AsNullable(), TemperatureAlertCategory);
    }

    private void EnqueueDamage(Entity<TemperatureDamageComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (ShouldUpdateDamage.Add(ent) && !ent.Comp.TakingDamage)
            ent.Comp.LastUpdate = _gameTiming.CurTime;
    }

    private void OnUnpaused(Entity<TemperatureDamageComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.LastUpdate += args.PausedTime;
    }

    private void OnParentChange(Entity<TemperatureDamageComponent> entity, ref EntParentChangedMessage args)
    {
        // We only need to update thresholds if the thresholds changed for the entity's ancestors.
        var oldThresholds = args.OldParent != null
            ? RecalculateParentThresholds(args.OldParent.Value)
            : (null, null);
        var xform = Transform(entity.Owner);
        var newThresholds = RecalculateParentThresholds(xform.ParentUid);

        if (oldThresholds != newThresholds)
            RecursiveThresholdUpdate((entity, entity.Comp, xform));
    }

    private void OnParentThresholdStartup(Entity<ContainerTemperatureComponent> entity, ref ComponentStartup args)
    {
        RecursiveThresholdUpdate(entity.Owner);
    }

    private void OnParentThresholdShutdown(Entity<ContainerTemperatureComponent> entity, ref ComponentShutdown args)
    {
        RecursiveThresholdUpdate(entity.Owner);
    }

    /// <summary>
    /// Recalculate and apply parent thresholds for the root entity and all its children.
    /// </summary>
    /// <param name="root">The root entity we're currently updating</param>
    private void RecursiveThresholdUpdate(Entity<TemperatureDamageComponent?, TransformComponent?> root)
    {
        RecalculateAndApplyParentThresholds(root);

        var xform = root.Comp2 ?? Transform(root);
        var enumerator = xform.ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            RecursiveThresholdUpdate(child);
        }
    }

    /// <summary>
    /// Recalculate parent thresholds and apply them on the uid temperature component.
    /// </summary>
    /// <param name="entity">The entity whose temperature damage thresholds we're updating</param>
    private void RecalculateAndApplyParentThresholds(Entity<TemperatureDamageComponent?> entity)
    {
        if (!_tempDamageQuery.Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        var newThresholds = RecalculateParentThresholds(Transform(entity).ParentUid);
        entity.Comp.ParentHeatDamageThreshold = newThresholds.Item1;
        entity.Comp.ParentColdDamageThreshold = newThresholds.Item2;
    }

    /// <summary>
    /// Recalculate Parent Heat/Cold DamageThreshold by recursively checking each ancestor and fetching the
    /// maximum HeatDamageThreshold and the minimum ColdDamageThreshold if any exists (aka the best value for each).
    /// </summary>
    /// <param name="initialParentUid">parent we start with</param>
    private (float?, float?) RecalculateParentThresholds(EntityUid initialParentUid)
    {
        // Recursively check parents for the best threshold available
        var parentUid = initialParentUid;
        float? newHeatThreshold = null;
        float? newColdThreshold = null;
        while (parentUid.IsValid())
        {
            if (_containerTemperatureQuery.TryComp(parentUid, out var newThresholds))
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

            parentUid = Transform(parentUid).ParentUid;
        }

        return (newHeatThreshold, newColdThreshold);
    }
}
