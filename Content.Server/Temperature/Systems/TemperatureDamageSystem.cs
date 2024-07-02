using Content.Server.Administration.Logs;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Database;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// The system responsible for handling the behaviour of entities that can take damage due to their temperature.
/// </summary>
public sealed partial class TemperatureDamageSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    private EntityQuery<DamageableComponent> _damageQuery = default!;
    private EntityQuery<TemperatureDamageThresholdsComponent> _thresholdsQuery = default!;
    private EntityQuery<ContainerTemperatureDamageThresholdsComponent> _containerThresholdsQuery = default!;

    /// <summary>
    /// All of the entities that will have the damage they are taking due to their temperature updated.
    /// </summary>
    /// <remarks>
    /// This is done because both AtmosExposed and Flammable call ChangeHeat in the same tick, meaning
    /// that we need some mechanism to ensure it doesn't double dip on damage for both calls.
    /// </remarks>
    private readonly HashSet<Entity<TemperatureDamageThresholdsComponent, TemperatureComponent>> _shouldUpdateDamage = new();

    /// <summary>
    /// The amount of time that should pass between temperature damage updates.
    /// </summary>
    public float UpdateInterval = 1.0f;

    /// <summary>
    /// The amount of time that has passed since the last time temperature damage was updated for entities.
    /// </summary>
    private float _accumulatedFrameTime;

    /// <summary>
    /// The alert category used for overheating/overcooling damage warnings.
    /// </summary>
    [ValidatePrototypeId<AlertCategoryPrototype>]
    public const string TemperatureAlertCategory = "Temperature";

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(TemperatureSystem)); // To conserve parity with old behaviour.

        _damageQuery = GetEntityQuery<DamageableComponent>();
        _thresholdsQuery = GetEntityQuery<TemperatureDamageThresholdsComponent>();
        _containerThresholdsQuery = GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>();

        SubscribeLocalEvent<TemperatureDamageThresholdsComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
        SubscribeLocalEvent<AlertsComponent, OnTemperatureChangeEvent>(OnTemperatureChange);

        // Allows overriding thresholds based on the parent's thresholds.
        SubscribeLocalEvent<TemperatureDamageThresholdsComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentStartup>(OnParentThresholdStartup);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentShutdown>(OnParentThresholdShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulatedFrameTime += frameTime;

        if (_accumulatedFrameTime < UpdateInterval)
            return;

        _accumulatedFrameTime -= UpdateInterval;

        if (_shouldUpdateDamage.Count <= 0)
            return;

        foreach (var (uid, thresholds, temperature) in _shouldUpdateDamage)
        {
            if (!EntityManager.MetaQuery.TryGetComponent(uid, out var metaData) || Deleted(uid, metaData) || Paused(uid, metaData))
                continue;

            UpdateDamage((uid, thresholds, temperature, null));
        }

        _shouldUpdateDamage.Clear();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    private void UpdateDamage(Entity<TemperatureDamageThresholdsComponent, TemperatureComponent, DamageableComponent?> entity)
    {
        var (uid, thresholds, temperature, damage) = entity;

        if (!_damageQuery.Resolve(uid, ref damage))
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
                thresholds.TakingDamage = true;
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking high temperature damage");
            }

            var diff = Math.Abs(temperature.CurrentTemperature - heatDamageThreshold);
            var tempDamage = c / (1 + a * Math.Pow(Math.E, -heatK * diff)) - y;
            _damageSystem.TryChangeDamage(uid, thresholds.HeatDamage * tempDamage, damageable: damage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (temperature.CurrentTemperature <= coldDamageThreshold)
        {
            if (!thresholds.TakingDamage)
            {
                thresholds.TakingDamage = true;
                _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} started taking low temperature damage");
            }

            var diff = Math.Abs(temperature.CurrentTemperature - coldDamageThreshold);
            var tempDamage =
                Math.Sqrt(diff * (Math.Pow(thresholds.DamageCap.Double(), 2) / coldDamageThreshold));
            _damageSystem.TryChangeDamage(uid, thresholds.ColdDamage * tempDamage, damageable: damage, ignoreResistances: true, interruptsDoAfters: false);
        }
        else if (thresholds.TakingDamage)
        {
            thresholds.TakingDamage = false;
            _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} stopped taking temperature damage");
        }
    }

    /// <summary>
    /// Recalculate and apply parent thresholds for the root entity and all its descendant.
    /// </summary>
    /// <param name="root"></param>
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
    /// <param name="uid"></param>
    private void RecalculateAndApplyParentThresholds(EntityUid uid)
    {
        if (!_thresholdsQuery.TryGetComponent(uid, out var thresholds))
            return;

        (thresholds.ParentHeatDamageThreshold, thresholds.ParentColdDamageThreshold)
            = RecalculateParentThresholds(Transform(uid).ParentUid);
    }

    /// <summary>
    /// Recalculate Parent Heat/Cold DamageThreshold by recursively checking each ancestor and fetching the
    /// maximum HeatDamageThreshold and the minimum ColdDamageThreshold if any exists (aka the best value for each).
    /// </summary>
    /// <param name="initialParentUid"></param>
    private (float?, float?) RecalculateParentThresholds(EntityUid initialParentUid)
    {
        // Recursively check parents for the best threshold available
        var parentUid = initialParentUid;
        float? newHeatThreshold = null;
        float? newColdThreshold = null;
        while (parentUid.IsValid())
        {
            if (_containerThresholdsQuery.TryGetComponent(parentUid, out var newThresholds))
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

            parentUid = Transform(parentUid).ParentUid;
        }

        return (newHeatThreshold, newColdThreshold);
    }

    #region Event Handlers

    /// <summary>
    /// Handles queueing temperature damage updates in response to the temperature of an entity changing.
    /// </summary>
    /// <param name="entity">An entity that can take damage in response to its temperature.</param>
    /// <param name="args">A change of the temperature of the given entity.</param>
    private void OnTemperatureChange(Entity<TemperatureDamageThresholdsComponent> entity, ref OnTemperatureChangeEvent args)
    {
        _shouldUpdateDamage.Add((entity.Owner, entity.Comp, args.Entity.Comp));
    }

    /// <summary>
    /// Handles recalculating the temperature damage thresholds imposed on an entity by its container.
    /// </summary>
    /// <param name="entity">An entity that can take damage in response to its temperature.</param>
    /// <param name="args">A change of the containing entity of the given entity.</param>
    private void OnParentChange(Entity<TemperatureDamageThresholdsComponent> entity, ref EntParentChangedMessage args)
    {
        // We only need to update thresholds if the thresholds changed for the entity's ancestors.
        var oldThresholds = args.OldParent != null
            ? RecalculateParentThresholds(args.OldParent.Value)
            : (null, null);

        var newThresholds = RecalculateParentThresholds(args.Transform.ParentUid);

        if (oldThresholds != newThresholds)
        {
            RecursiveThresholdUpdate(entity);
        }
    }

    /// <summary>
    /// Makes adding a <see cref="ContainerTemperatureDamageThresholdsComponent"/> to an entity properly give its damage thresholds to its contents.
    /// </summary>
    /// <param name="entity">The entity that has begun modifying its childrens temperature damage thresholds.</param>
    /// <param name="args">A unit event indicating that the component is starting up.</param>
    private void OnParentThresholdStartup(Entity<ContainerTemperatureDamageThresholdsComponent> entity, ref ComponentStartup args)
    {
        RecursiveThresholdUpdate(entity);
    }

    /// <summary>
    /// Makes adding a <see cref="ContainerTemperatureDamageThresholdsComponent"/> to an entity properly give its damage thresholds to its contents.
    /// </summary>
    /// <param name="entity">The entity that has ceased modifying its childrens temperature damage thresholds.</param>
    /// <param name="args">A unit event indicating that the component is shutting down.</param>
    private void OnParentThresholdShutdown(Entity<ContainerTemperatureDamageThresholdsComponent> entity, ref ComponentShutdown args)
    {
        RecursiveThresholdUpdate(entity);
    }

    #endregion Event Handlers
}
