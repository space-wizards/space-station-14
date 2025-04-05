using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Rounding;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed partial class StaminaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    /// <summary>
    /// How much of a buffer is there between the stun duration and when stuns can be re-applied.
    /// </summary>
    private static readonly TimeSpan StamCritBufferTime = TimeSpan.FromSeconds(3f);

    public float UniversalStaminaDamageModifier { get; private set; } = 1f;

    public override void Initialize()
    {
        base.Initialize();

        InitializeModifier();

        SubscribeLocalEvent<StaminaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StaminaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StaminaComponent, AfterAutoHandleStateEvent>(OnStamHandleState);
        SubscribeLocalEvent<StaminaComponent, DisarmedEvent>(OnDisarmed);
        SubscribeLocalEvent<StaminaComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<StaminaDamageOnEmbedComponent, EmbedEvent>(OnProjectileEmbed);

        SubscribeLocalEvent<StaminaDamageOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<StaminaDamageOnCollideComponent, ThrowDoHitEvent>(OnThrowHit);

        SubscribeLocalEvent<StaminaDamageOnHitComponent, MeleeHitEvent>(OnMeleeHit);

        Subs.CVar(_config, CCVars.PlaytestStaminaDamageModifier, value => UniversalStaminaDamageModifier = value, true);
    }

    private void OnStamHandleState(EntityUid uid, StaminaComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.Critical)
            EnterStamCrit(uid, component);
        else
        {
            if (component.StaminaDamage > 0f)
                EnsureComp<ActiveStaminaComponent>(uid);

            ExitStamCrit(uid, component);
        }
    }

    private void OnShutdown(EntityUid uid, StaminaComponent component, ComponentShutdown args)
    {
        if (MetaData(uid).EntityLifeStage < EntityLifeStage.Terminating)
        {
            RemCompDeferred<ActiveStaminaComponent>(uid);
        }
        _alerts.ClearAlert(uid, component.StaminaAlert);
    }

    private void OnStartup(EntityUid uid, StaminaComponent component, ComponentStartup args)
    {
        SetStaminaAlert(uid, component);
    }

    [PublicAPI]
    public float GetStaminaDamage(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0f;

        var curTime = _timing.CurTime;
        var pauseTime = _metadata.GetPauseTime(uid);
        return MathF.Max(0f, component.StaminaDamage - MathF.Max(0f, (float)(curTime - (component.NextUpdate + pauseTime)).TotalSeconds * component.Decay));
    }

    private void OnRejuvenate(EntityUid uid, StaminaComponent component, RejuvenateEvent args)
    {
        if (component.StaminaDamage >= component.CritThreshold)
        {
            ExitStamCrit(uid, component);
        }

        component.StaminaDamage = 0;
        RemComp<ActiveStaminaComponent>(uid);
        SetStaminaAlert(uid, component);
        Dirty(uid, component);
    }

    private void OnDisarmed(EntityUid uid, StaminaComponent component, DisarmedEvent args)
    {
        if (args.Handled)
            return;

        if (component.Critical)
            return;

        var damage = args.PushProbability * component.CritThreshold;
        TakeStaminaDamage(uid, damage, component, source: args.Source);

        args.PopupPrefix = "disarm-action-shove-";
        args.IsStunned = component.Critical;

        args.Handled = true;
    }

    private void OnMeleeHit(EntityUid uid, StaminaDamageOnHitComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            component.Damage <= 0f)
        {
            return;
        }

        var ev = new StaminaDamageOnHitAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        var stamQuery = GetEntityQuery<StaminaComponent>();
        var toHit = new List<(EntityUid Entity, StaminaComponent Component)>();

        // Split stamina damage between all eligible targets.
        foreach (var ent in args.HitEntities)
        {
            if (!stamQuery.TryGetComponent(ent, out var stam))
                continue;

            toHit.Add((ent, stam));
        }

        var hitEvent = new StaminaMeleeHitEvent(toHit);
        RaiseLocalEvent(uid, hitEvent);

        if (hitEvent.Handled)
            return;

        var damage = component.Damage;

        damage *= hitEvent.Multiplier;

        damage += hitEvent.FlatModifier;

        foreach (var (ent, comp) in toHit)
        {
            TakeStaminaDamage(ent, damage / toHit.Count, comp, source: args.User, with: args.Weapon, sound: component.Sound);
        }
    }

    private void OnProjectileHit(EntityUid uid, StaminaDamageOnCollideComponent component, ref ProjectileHitEvent args)
    {
        OnCollide(uid, component, args.Target);
    }

    private void OnProjectileEmbed(EntityUid uid, StaminaDamageOnEmbedComponent component, ref EmbedEvent args)
    {
        if (!TryComp<StaminaComponent>(args.Embedded, out var stamina))
            return;

        TakeStaminaDamage(args.Embedded, component.Damage, stamina, source: uid);
    }

    private void OnThrowHit(EntityUid uid, StaminaDamageOnCollideComponent component, ThrowDoHitEvent args)
    {
        OnCollide(uid, component, args.Target);
    }

    private void OnCollide(EntityUid uid, StaminaDamageOnCollideComponent component, EntityUid target)
    {
        // you can't inflict stamina damage on things with no stamina component
        // this prevents stun batons from using up charges when throwing it at lockers or lights
        if (!HasComp<StaminaComponent>(target))
            return;

        var ev = new StaminaDamageOnHitAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        TakeStaminaDamage(target, component.Damage, source: uid, sound: component.Sound);
    }

    private void SetStaminaAlert(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.CritThreshold - component.StaminaDamage), component.CritThreshold, 7);
        _alerts.ShowAlert(uid, component.StaminaAlert, (short)severity);
    }

    /// <summary>
    /// Tries to take stamina damage without raising the entity over the crit threshold.
    /// </summary>
    public bool TryTakeStamina(EntityUid uid, float value, StaminaComponent? component = null, EntityUid? source = null, EntityUid? with = null)
    {
        // Something that has no Stamina component automatically passes stamina checks
        if (!Resolve(uid, ref component, false))
            return true;

        var oldStam = component.StaminaDamage;

        if (oldStam + value > component.CritThreshold || component.Critical)
            return false;

        TakeStaminaDamage(uid, value, component, source, with, visual: false);
        return true;
    }

    public void TakeStaminaDamage(EntityUid uid, float value, StaminaComponent? component = null,
        EntityUid? source = null, EntityUid? with = null, bool visual = true, SoundSpecifier? sound = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var ev = new BeforeStaminaDamageEvent(value);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        value = UniversalStaminaDamageModifier * value;

        // Have we already reached the point of max stamina damage?
        if (component.Critical)
            return;



        Log.Debug($"[TSD] value: {value}, uid: {ToPrettyString(uid)}, source: {ToPrettyString(source)}, with: {ToPrettyString(with)}");
        var oldDamage = component.StaminaDamage;
        Log.Debug($"[TSD] oldDamage: {value}");
        component.StaminaDamage = MathF.Max(0f, component.StaminaDamage + value);
        Log.Debug($"[TSD] new StaminaDamage is: {component.StaminaDamage}");

        // Reset the decay cooldown upon taking damage.
        if (oldDamage < component.StaminaDamage)
        {
            Log.Debug($"[TSD] oldDamage({oldDamage}) < StaminaDamage({component.StaminaDamage})");

            var nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(component.Cooldown);
            Log.Debug($"[TSD] nextUpdate: {nextUpdate}");

            Log.Debug($"[TSD] component.NextUpdate({component.NextUpdate}) is " + (component.NextUpdate < nextUpdate ? "greater" : "less") + $" than nextUpdate({nextUpdate})");
            if (component.NextUpdate < nextUpdate)
            {
                Log.Debug($"[TSD] Assigning value of nextUpdate({nextUpdate}) to component.NextUpdate");
                component.NextUpdate = nextUpdate;
            }
        }

        //var slowdownThreshold = component.CritThreshold / 2f;

        //If we go above n % then apply slowdown
        //if (oldDamage < slowdownThreshold &&
        //    component.StaminaDamage > slowdownThreshold)
        //{
        //    _stunSystem.TrySlowdown(uid, TimeSpan.FromSeconds(3), true, 0.8f, 0.8f);
        //}

        AdjustSpeed(uid, component);

        SetStaminaAlert(uid, component);

        if (!component.Critical)
        {
            if (component.StaminaDamage >= component.CritThreshold)
            {
                EnterStamCrit(uid, component);
            }
        }
        else
        {
            if (component.StaminaDamage < component.CritThreshold)
            {
                ExitStamCrit(uid, component);
            }
        }

        EnsureComp<ActiveStaminaComponent>(uid);
        Dirty(uid, component);

        if (value <= 0)
            return;
        if (source != null)
        {
            _adminLogger.Add(LogType.Stamina, $"{ToPrettyString(source.Value):user} caused {value} stamina damage to {ToPrettyString(uid):target}{(with != null ? $" using {ToPrettyString(with.Value):using}" : "")}");
        }
        else
        {
            _adminLogger.Add(LogType.Stamina, $"{ToPrettyString(uid):target} took {value} stamina damage");
        }

        if (visual)
        {
            _color.RaiseEffect(Color.Aqua, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));
        }

        if (_net.IsServer)
        {
            _audio.PlayPvs(sound, uid);
        }
    }

    public override void Update(float frameTime) // FLAG
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var stamQuery = GetEntityQuery<StaminaComponent>();
        var query = EntityQueryEnumerator<ActiveStaminaComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out _))
        {
            // Just in case we have active but not stamina we'll check and account for it.
            if (!stamQuery.TryGetComponent(uid, out var comp) ||
                comp.StaminaDamage <= 0f && !comp.Critical)
            {
                Log.Debug($"[U] Removing ActiveStaminaComponent from uid: {ToPrettyString(uid)}");
                RemComp<ActiveStaminaComponent>(uid);
                continue;
            }

            // Shouldn't need to consider paused time as we're only iterating non-paused stamina components.
            var nextUpdate = comp.NextUpdate;

            if (nextUpdate > curTime)
                continue;

            // We were in crit so come out of it and continue.
            if (comp.Critical)
            {
                ExitStamCrit(uid, comp);
                continue;
            }

            comp.NextUpdate += TimeSpan.FromSeconds(1f);
            TakeStaminaDamage(uid, -comp.Decay, comp);
            Dirty(uid, comp);
        }
    }

    private void EnterStamCrit(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            component.Critical)
        {
            return;
        }

        // To make the difference between a stun and a stamcrit clear
        // TODO: Mask?

        component.Critical = true;
        component.StaminaDamage = component.CritThreshold;

        _stunSystem.TryParalyze(uid, component.StunTime, true);

        // Give them buffer before being able to be re-stunned
        component.NextUpdate = _timing.CurTime + component.StunTime + StamCritBufferTime;
        EnsureComp<ActiveStaminaComponent>(uid);
        Dirty(uid, component);
        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} entered stamina crit");
        Log.Debug($"[EnterSC] {ToPrettyString(uid):user} entered stamina crit");
    }

    private void ExitStamCrit(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            !component.Critical)
        {
            return;
        }

        component.Critical = false;
        component.StaminaDamage = 0f;
        component.NextUpdate = _timing.CurTime; // Возможно тут кроется проблема с остановкой уровня стамины на прежнем уровне.
        Log.Debug("[ExisSC] Resetting speed to its normal");
        AdjustSpeed(uid, component, true);
        Log.Debug($"[ExitSC] NextUpdate: {component.NextUpdate}");
        SetStaminaAlert(uid, component);
        RemComp<ActiveStaminaComponent>(uid);
        Dirty(uid, component);
        _adminLogger.Add(LogType.Stamina, LogImpact.Low, $"{ToPrettyString(uid):user} recovered from stamina crit");
        Log.Debug($"{ToPrettyString(uid):user} recovered from stamina crit");
    }

    private void AdjustSpeed(EntityUid uid, StaminaComponent? comp = null, bool recover = false)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (recover)
        {
            Log.Debug($"[AS] Recovering speed to 1f");
            _stunSystem.UpdateMovementModifiers(uid, 1f, comp);
            return;
        }

        var thresholds = HasComp<SlowOnDamageComponent>(uid)
            ? Comp<SlowOnDamageComponent>(uid).SpeedModifierThresholds
            : new Dictionary<FixedPoint2, float> { { 60, 0.7f }, { 80, 0.5f } };

        var closest = FixedPoint2.Zero;

        string dbgm = "[AS] Thresholds: {";
        foreach (var thres in thresholds)
        {
            dbgm += $"{thres.Key} - {thres.Value},";
        }
        dbgm += "}";
        Log.Debug(dbgm);

        foreach (var thres in thresholds)
        {
            var key = thres.Key.Float();

            if (comp.StaminaDamage >= key && key > closest && closest < comp.CritThreshold)
                closest = thres.Key;
        }

        Log.Debug($"[AS] Closest threshold: {closest}");

        if (closest != FixedPoint2.Zero)
        {
            Log.Debug($"[AS] Changing speed to: {thresholds[closest]}");
            _stunSystem.UpdateMovementModifiers(uid, thresholds[closest], comp);
        }
        else if (closest == FixedPoint2.Zero)
        {
            Log.Debug($"[AS] Adjusting speed to: 1.0");
            _stunSystem.UpdateMovementModifiers(uid, 1f, comp);
        }
    }
}

/// <summary>
///     Raised before stamina damage is dealt to allow other systems to cancel it.
/// </summary>
[ByRefEvent]
public record struct BeforeStaminaDamageEvent(float Value, bool Cancelled = false);
