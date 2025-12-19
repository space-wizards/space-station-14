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
using System.Linq;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles entities taking damage from being too hot or too cold.
/// Also handles alerts relevant to the same.
/// </summary>
public sealed partial class TemperatureDamageSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <summary>
    ///     All the components that will have their damage updated at the end of the tick.
    ///     This is done because both AtmosExposed and Flammable call ChangeHeat in the same tick, meaning
    ///     that we need some mechanism to ensure it doesn't double dip on damage for both calls.
    /// </summary>
    public HashSet<Entity<TemperatureDamageComponent>> ShouldUpdateDamage = new();

    /// <summary>
    ///
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

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(TemperatureSystem));

        SubscribeLocalEvent<AlertsComponent, OnTemperatureChangeEvent>(ServerAlert);

        SubscribeLocalEvent<TemperatureDamageComponent, OnTemperatureChangeEvent>(EnqueueDamage);
        SubscribeLocalEvent<TemperatureDamageComponent, EntityUnpausedEvent>(OnUnpaused);

        // Allows overriding thresholds based on the parent's thresholds.
        SubscribeLocalEvent<TemperatureDamageComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentStartup>(
            OnParentThresholdStartup);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentShutdown>(
            OnParentThresholdShutdown);
    }

    public override void Update(float frameTime)
    {
        if (!ShouldUpdateDamage.Any())
            return;

        var curTime = _gameTiming.CurTime;
        foreach (var (uid, thresholds) in ShouldUpdateDamage)
        {
            if (Deleted(uid) || Paused(uid))
                continue;

            var deltaTime = curTime - thresholds.LastUpdate;
            if (thresholds.TakingDamage && deltaTime < thresholds.UpdateInterval)
                continue;

            ChangeDamage(uid, thresholds, deltaTime);
        }

        ShouldUpdateDamage.Clear();
    }

    private void ChangeDamage(EntityUid uid, TemperatureDamageComponent thresholds, TimeSpan deltaTime)
    {
        thresholds.LastUpdate = _gameTiming.CurTime;

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
            _damageable.TryChangeDamage(uid, thresholds.HeatDamage * tempDamage * deltaTime.TotalSeconds, ignoreResistances: true, interruptsDoAfters: false);
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
            _damageable.TryChangeDamage(uid, thresholds.ColdDamage * tempDamage * deltaTime.TotalSeconds, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (thresholds.TakingDamage)
        {
            _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} stopped taking temperature damage");
            thresholds.TakingDamage = false;
        }
    }

    private void ServerAlert(EntityUid uid, AlertsComponent status, OnTemperatureChangeEvent args)
    {
        ProtoId<AlertPrototype> type;
        float threshold;
        float idealTemp;

        if (!TryComp<TemperatureDamageComponent>(uid, out var thresholds))
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

        // Calculates a scale where 0.0 is the ideal temperature and 1.0 is where temperature damage begins
        // The cold and hot scales will differ in their range if the ideal temperature is not exactly halfway between the thresholds
        var tempScale = (args.CurrentTemperature - idealTemp) / (threshold - idealTemp);
        var alertLevel = (short)ContentHelpers.RoundToLevels(tempScale - MinAlertTemperatureScale, 1.00f - MinAlertTemperatureScale, MaxTemperatureAlertSeverity + 1);

        if (alertLevel > 0)
            _alerts.ShowAlert(uid, type, alertLevel);
        else
            _alerts.ClearAlertCategory(uid, TemperatureAlertCategory);
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

    private void OnParentChange(EntityUid uid, TemperatureDamageComponent component,
        ref EntParentChangedMessage args)
    {
        var thresholdsQuery = GetEntityQuery<TemperatureDamageComponent>();
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
        RecursiveThresholdUpdate(uid, GetEntityQuery<TemperatureDamageComponent>(), GetEntityQuery<TransformComponent>(),
            GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>());
    }

    private void OnParentThresholdShutdown(EntityUid uid, ContainerTemperatureDamageThresholdsComponent component,
        ComponentShutdown args)
    {
        RecursiveThresholdUpdate(uid, GetEntityQuery<TemperatureDamageComponent>(), GetEntityQuery<TransformComponent>(),
            GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>());
    }

    /// <summary>
    /// Recalculate and apply parent thresholds for the root entity and all its descendant.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="thresholdsQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="containerThresholdsQuery"></param>
    private void RecursiveThresholdUpdate(EntityUid root, EntityQuery<TemperatureDamageComponent> thresholdsQuery,
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
    /// <param name="thresholdsQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="containerThresholdsQuery"></param>
    private void RecalculateAndApplyParentThresholds(EntityUid uid,
        EntityQuery<TemperatureDamageComponent> thresholdsQuery, EntityQuery<TransformComponent> transformQuery,
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
    /// <param name="containerThresholdsQuery"></param>
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
