using Content.Shared.Administration.Logs;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Shared.Anomaly;

public abstract class SharedAnomalySystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLog = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<AnomalyComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<AnomalyComponent, MeleeThrowOnHitStartEvent>(OnAnomalyThrowStart);
        SubscribeLocalEvent<AnomalyComponent, MeleeThrowOnHitEndEvent>(OnAnomalyThrowEnd);
    }

    private void OnInteractHand(EntityUid uid, AnomalyComponent component, InteractHandEvent args)
    {
        DoAnomalyBurnDamage(uid, args.User, component);
        args.Handled = true;
    }

    private void OnAttacked(EntityUid uid, AnomalyComponent component, AttackedEvent args)
    {
        if (HasComp<CorePoweredThrowerComponent>(args.Used))
            return;

        DoAnomalyBurnDamage(uid, args.User, component);
    }

    private void OnAnomalyThrowStart(Entity<AnomalyComponent> ent, ref MeleeThrowOnHitStartEvent args)
    {
        if (!TryComp<CorePoweredThrowerComponent>(args.Used, out var corePowered) || !TryComp<PhysicsComponent>(ent, out var body))
            return;
        _physics.SetBodyType(ent, BodyType.Dynamic, body: body);
        ChangeAnomalyStability(ent, Random.NextFloat(corePowered.StabilityPerThrow.X, corePowered.StabilityPerThrow.Y), ent.Comp);
    }

    private void OnAnomalyThrowEnd(Entity<AnomalyComponent> ent, ref MeleeThrowOnHitEndEvent args)
    {
        _physics.SetBodyType(ent, BodyType.Static);
    }

    public void DoAnomalyBurnDamage(EntityUid source, EntityUid target, AnomalyComponent component)
    {
        _damageable.TryChangeDamage(target, component.AnomalyContactDamage, true);
        if (!Timing.IsFirstTimePredicted || _net.IsServer)
            return;
        Audio.PlayPvs(component.AnomalyContactDamageSound, source);
        Popup.PopupEntity(Loc.GetString("anomaly-component-contact-damage"), target, target);
    }

    public void DoAnomalyPulse(EntityUid uid, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        DebugTools.Assert(component.MinPulseLength > TimeSpan.FromSeconds(3)); // this is just to prevent lagspikes mispredicting pulses
        RefreshPulseTimer(uid, component);

        if (_net.IsServer)
            Log.Info($"Performing anomaly pulse. Entity: {ToPrettyString(uid)}");

        // if we are above the growth threshold, then grow before the pulse
        if (component.Stability > component.GrowthThreshold)
        {
            ChangeAnomalySeverity(uid, GetSeverityIncreaseFromGrowth(component), component);
        }

        var minStability = component.PulseStabilityVariation.X * component.Severity;
        var maxStability = component.PulseStabilityVariation.Y * component.Severity;
        var stability = Random.NextFloat(minStability, maxStability);
        ChangeAnomalyStability(uid, stability, component);

        AdminLog.Add(LogType.Anomaly, LogImpact.Medium, $"Anomaly {ToPrettyString(uid)} pulsed with severity {component.Severity}.");
        if (_net.IsServer)
            Audio.PlayPvs(component.PulseSound, uid);

        var pulse = EnsureComp<AnomalyPulsingComponent>(uid);
        pulse.EndTime  = Timing.CurTime + pulse.PulseDuration;
        Appearance.SetData(uid, AnomalyVisuals.IsPulsing, true);

        var powerMod = 1f;
        if (component.CurrentBehavior != null)
        {
            var beh = _prototype.Index<AnomalyBehaviorPrototype>(component.CurrentBehavior);
            powerMod = beh.PulsePowerModifier;
        }
        var ev = new AnomalyPulseEvent(uid, component.Stability, component.Severity, powerMod);
        RaiseLocalEvent(uid, ref ev, true);
    }

    public void RefreshPulseTimer(EntityUid uid, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var variation = Random.NextFloat(-component.PulseVariation, component.PulseVariation) + 1;
        component.NextPulseTime = Timing.CurTime + GetPulseLength(component) * variation;
    }

    /// <summary>
    /// Begins the animation for going supercritical
    /// </summary>
    /// <param name="uid"></param>
    public void StartSupercriticalEvent(EntityUid uid)
    {
        // don't restart it if it's already begun
        if (HasComp<AnomalySupercriticalComponent>(uid))
            return;

        AdminLog.Add(LogType.Anomaly, LogImpact.Extreme, $"Anomaly {ToPrettyString(uid)} began to go supercritical.");
        if (_net.IsServer)
            Log.Info($"Anomaly is going supercritical. Entity: {ToPrettyString(uid)}");

        var super = AddComp<AnomalySupercriticalComponent>(uid);
        super.EndTime = Timing.CurTime + super.SupercriticalDuration;
        Appearance.SetData(uid, AnomalyVisuals.Supercritical, true);
        Dirty(uid, super);
    }

    /// <summary>
    /// Does the supercritical event for the anomaly.
    /// This isn't called once the anomaly reaches the point, but
    /// after the animation for it going supercritical
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void DoAnomalySupercriticalEvent(EntityUid uid, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        Audio.PlayPvs(component.SupercriticalSound, uid);

        if (_net.IsServer)
            Log.Info($"Raising supercritical event. Entity: {ToPrettyString(uid)}");

        var powerMod = 1f;
        if (component.CurrentBehavior != null)
        {
            var beh = _prototype.Index<AnomalyBehaviorPrototype>(component.CurrentBehavior);
            powerMod = beh.PulsePowerModifier;
        }

        var ev = new AnomalySupercriticalEvent(uid, powerMod);
        RaiseLocalEvent(uid, ref ev, true);

        EndAnomaly(uid, component, true);
    }

    /// <summary>
    /// Ends an anomaly, cleaning up all entities that may be associated with it.
    /// </summary>
    /// <param name="uid">The anomaly being shut down</param>
    /// <param name="component"></param>
    /// <param name="supercritical">Whether or not the anomaly ended via supercritical event</param>
    public void EndAnomaly(EntityUid uid, AnomalyComponent? component = null, bool supercritical = false)
    {
        // Logging before resolve, in case the anomaly has deleted itself.
        if (_net.IsServer)
            Log.Info($"Ending anomaly. Entity: {ToPrettyString(uid)}");
        AdminLog.Add(LogType.Anomaly, supercritical ? LogImpact.High : LogImpact.Low,
                     $"Anomaly {ToPrettyString(uid)} {(supercritical ? "went supercritical" : "decayed")}.");

        if (!Resolve(uid, ref component))
            return;

        var ev = new AnomalyShutdownEvent(uid, supercritical);
        RaiseLocalEvent(uid, ref ev, true);

        if (Terminating(uid) || _net.IsClient)
            return;

        Spawn(supercritical ? component.CorePrototype : component.CoreInertPrototype, Transform(uid).Coordinates);

        QueueDel(uid);
    }

    /// <summary>
    /// Changes the stability of the anomaly.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="change"></param>
    /// <param name="component"></param>
    public void ChangeAnomalyStability(EntityUid uid, float change, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var newVal = component.Stability + change;

        component.Stability = Math.Clamp(newVal, 0, 1);
        Dirty(uid, component);

        var ev = new AnomalyStabilityChangedEvent(uid, component.Stability, component.Severity);
        RaiseLocalEvent(uid, ref ev, true);
    }

    /// <summary>
    /// Changes the severity of an anomaly, going supercritical if it exceeds 1.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="change"></param>
    /// <param name="component"></param>
    public void ChangeAnomalySeverity(EntityUid uid, float change, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var newVal = component.Severity + change;

        if (newVal >= 1)
            StartSupercriticalEvent(uid);

        component.Severity = Math.Clamp(newVal, 0, 1);
        Dirty(uid, component);

        var ev = new AnomalySeverityChangedEvent(uid, component.Stability, component.Severity);
        RaiseLocalEvent(uid, ref ev, true);
    }

    /// <summary>
    /// Changes the health of an anomaly, ending it if it's less than 0.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="change"></param>
    /// <param name="component"></param>
    public void ChangeAnomalyHealth(EntityUid uid, float change, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var newVal = component.Health + change;

        if (newVal < 0)
        {
            EndAnomaly(uid, component);
            return;
        }

        component.Health = Math.Clamp(newVal, 0, 1);
        Dirty(uid, component);

        var ev = new AnomalyHealthChangedEvent(uid, component.Health);
        RaiseLocalEvent(uid, ref ev, true);
    }

    /// <summary>
    /// Gets the length of time between each pulse
    /// for an anomaly based on its current stability.
    /// </summary>
    /// <remarks>
    /// For anomalies under the instability theshold, this will return the maximum length.
    /// For those over the theshold, they will return an amount between the maximum and
    /// minium value based on a linear relationship with the stability.
    /// </remarks>
    /// <param name="component"></param>
    /// <returns>The length of time as a TimeSpan, not including random variation.</returns>
    public TimeSpan GetPulseLength(AnomalyComponent component)
    {
        DebugTools.Assert(component.MaxPulseLength > component.MinPulseLength);
        var modifier = Math.Clamp((component.Stability - component.GrowthThreshold) / component.GrowthThreshold, 0, 1);

        var lenght = (component.MaxPulseLength - component.MinPulseLength) * modifier + component.MinPulseLength;

        //Apply behavior modifier
        if (component.CurrentBehavior != null)
        {
            var behavior = _prototype.Index(component.CurrentBehavior.Value);
            lenght *= behavior.PulseFrequencyModifier;
        }
        return lenght;
    }

    /// <summary>
    /// Gets the increase in an anomaly's severity due
    /// to being above its growth threshold
    /// </summary>
    /// <param name="component"></param>
    /// <returns>The increase in severity for this anomaly</returns>
    private float GetSeverityIncreaseFromGrowth(AnomalyComponent component)
    {
        var score = 1 + Math.Max(component.Stability - component.GrowthThreshold, 0) * 10;
        return score * component.SeverityGrowthCoefficient;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var anomalyQuery = EntityQueryEnumerator<AnomalyComponent>();
        while (anomalyQuery.MoveNext(out var ent, out var anomaly))
        {
            // if the stability is under the death threshold,
            // update it every second to start killing it slowly.
            if (anomaly.Stability < anomaly.DecayThreshold)
            {
                ChangeAnomalyHealth(ent, anomaly.HealthChangePerSecond * frameTime, anomaly);
            }

            if (Timing.CurTime > anomaly.NextPulseTime)
            {
                DoAnomalyPulse(ent, anomaly);
            }
        }

        var pulseQuery = EntityQueryEnumerator<AnomalyPulsingComponent>();
        while (pulseQuery.MoveNext(out var ent, out var pulse))
        {
            if (Timing.CurTime > pulse.EndTime)
            {
                Appearance.SetData(ent, AnomalyVisuals.IsPulsing, false);
                RemComp(ent, pulse);
            }
        }

        var supercriticalQuery = EntityQueryEnumerator<AnomalySupercriticalComponent, AnomalyComponent>();
        while (supercriticalQuery.MoveNext(out var ent, out var super, out var anom))
        {
            if (Timing.CurTime <= super.EndTime)
                continue;
            DoAnomalySupercriticalEvent(ent, anom);
            RemComp(ent, super);
        }
    }

    /// <summary>
    /// Gets random points around the anomaly based on the given parameters.
    /// </summary>
    public List<TileRef>? GetSpawningPoints(EntityUid uid, float stability, float severity, AnomalySpawnSettings settings, float powerModifier = 1f)
    {
        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var amount = (int) (MathHelper.Lerp(settings.MinAmount, settings.MaxAmount, severity * stability * powerModifier) + 0.5f);

        var localpos = xform.Coordinates.Position;
        var tilerefs = grid.GetLocalTilesIntersecting(
            new Box2(localpos + new Vector2(-settings.MaxRange, -settings.MaxRange), localpos + new Vector2(settings.MaxRange, settings.MaxRange))).ToList();

        if (tilerefs.Count == 0)
            return null;

        var physQuery = GetEntityQuery<PhysicsComponent>();
        var resultList = new List<TileRef>();
        while (resultList.Count < amount)
        {
            if (tilerefs.Count == 0)
                break;

            var tileref = _random.Pick(tilerefs);
            var distance = MathF.Sqrt(MathF.Pow(tileref.X - xform.LocalPosition.X, 2) + MathF.Pow(tileref.Y - xform.LocalPosition.Y, 2));

            //cut outer & inner circle
            if (distance > settings.MaxRange || distance < settings.MinRange)
            {
                tilerefs.Remove(tileref);
                continue;
            }

            if (!settings.CanSpawnOnEntities)
            {
                var valid = true;
                foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
                {
                    if (!physQuery.TryGetComponent(ent, out var body))
                        continue;

                    if (body.BodyType != BodyType.Static ||
                        !body.Hard ||
                        (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                        continue;

                    valid = false;
                    break;
                }
                if (!valid)
                {
                    tilerefs.Remove(tileref);
                    continue;
                }
            }

            resultList.Add(tileref);
        }
        return resultList;
    }
}

[DataRecord]
public partial record struct AnomalySpawnSettings()
{
    /// <summary>
    /// should entities block spawning?
    /// </summary>
    public bool CanSpawnOnEntities { get; set; } = false;

    /// <summary>
    /// The minimum number of entities that spawn per pulse
    /// </summary>
    public int MinAmount { get; set; } = 0;

    /// <summary>
    /// The maximum number of entities that spawn per pulse
    /// scales with severity.
    /// </summary>
    public int MaxAmount { get; set; } = 1;

    /// <summary>
    /// The distance from the anomaly in which the entities will not appear
    /// </summary>
    public float MinRange { get; set; } = 0f;

    /// <summary>
    /// The maximum radius the entities will spawn in.
    /// </summary>
    public float MaxRange { get; set; } = 1f;

    /// <summary>
    /// Whether or not anomaly spawns entities on Pulse
    /// </summary>
    public bool SpawnOnPulse { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns entities on SuperCritical
    /// </summary>
    public bool SpawnOnSuperCritical { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns entities when destroyed
    /// </summary>
    public bool SpawnOnShutdown { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns entities on StabilityChanged
    /// </summary>
    public bool SpawnOnStabilityChanged { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns entities on SeverityChanged
    /// </summary>
    public bool SpawnOnSeverityChanged { get; set; } = false;
}
