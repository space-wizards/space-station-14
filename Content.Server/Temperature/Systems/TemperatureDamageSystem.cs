using Content.Server.Administration.Logs;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Database;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureDamageSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    private EntityQuery<TransformComponent> _xformQuery = default!;
    private EntityQuery<DamageableComponent> _damageQuery = default!;
    private EntityQuery<TemperatureComponent> _temperatureQuery = default!;
    private EntityQuery<TemperatureDamageThresholdsComponent> _thresholdsQuery = default!;
    private EntityQuery<ContainerTemperatureDamageThresholdsComponent> _containerThresholdsQuery = default!;

    /// <summary>
    /// All of the entities that will be processed during the next temperature damage tick.
    /// Exists because things can change an entities temperature multiple times per tick and we want to avoid double dipping on damage.
    /// </summary>
    private readonly HashSet<Entity<TemperatureDamageThresholdsComponent, TemperatureComponent>> _shouldUpdateDamage = new();

    /// <summary>
    /// The amount of time that has passed since the last temperature damage tick.
    /// </summary>
    private float _accumulatedFrameTime = 0f;

    /// <summary>
    /// The amount of time between processing temperature damage ticks.
    /// </summary>
    public float UpdateInterval = 1.0f;

    /// <summary>
    /// The alert catagory used to warn when a player is taking damage, or is in danger of doing so, due to low/high temperatures.
    /// </summary>
    [ValidatePrototypeId<AlertCategoryPrototype>]
    public const string TemperatureAlertCategory = "Temperature";

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(TemperatureSystem));

        _xformQuery = GetEntityQuery<TransformComponent>();
        _damageQuery = GetEntityQuery<DamageableComponent>();
        _temperatureQuery = GetEntityQuery<TemperatureComponent>();
        _thresholdsQuery = GetEntityQuery<TemperatureDamageThresholdsComponent>();
        _containerThresholdsQuery = GetEntityQuery<ContainerTemperatureDamageThresholdsComponent>();

        SubscribeLocalEvent<TemperatureDamageThresholdsComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
        SubscribeLocalEvent<AlertsComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);

        // Allows overriding thresholds based on the parent's thresholds.
        SubscribeLocalEvent<TemperatureDamageThresholdsComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentStartup>(OnParentThresholdStartup);
        SubscribeLocalEvent<ContainerTemperatureDamageThresholdsComponent, ComponentShutdown>(OnParentThresholdShutdown);
    }

    public override void Update(float frameTime)
    {
        _accumulatedFrameTime += frameTime;

        if (_accumulatedFrameTime < UpdateInterval)
            return;

        _accumulatedFrameTime -= UpdateInterval;

        if (_shouldUpdateDamage.Count <= 0)
            return;

        foreach (var entity in _shouldUpdateDamage)
        {
            if (Deleted(entity.Owner) || Paused(entity.Owner))
                continue;

            UpdateDamage(entity, UpdateInterval);
        }

        _shouldUpdateDamage.Clear();
    }

    private void UpdateDamage(Entity<TemperatureDamageThresholdsComponent, TemperatureComponent> entity, float updateInterval)
    {
        var (uid, thresholds, temperature) = entity;

        if (!_damageQuery.TryComp(uid, out var damage))
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
            _damageableSystem.TryChangeDamage(uid, thresholds.HeatDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false, damageable: damage);
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
            _damageableSystem.TryChangeDamage(uid, thresholds.ColdDamage * tempDamage, ignoreResistances: true, interruptsDoAfters: false, damageable: damage);
        }
        else if (thresholds.TakingDamage)
        {
            _adminLogger.Add(LogType.Temperature, $"{ToPrettyString(uid):entity} stopped taking temperature damage");
            thresholds.TakingDamage = false;
        }
    }

    private void OnTemperatureChanged(Entity<TemperatureDamageThresholdsComponent> entity, ref OnTemperatureChangeEvent args)
    {
        // Enqueue damage tick.
        _shouldUpdateDamage.Add((entity.Owner, entity.Comp, _temperatureQuery.GetComponent(entity.Owner)));
    }
}
