using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Rounding;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class StaminaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    private const string CollideFixture = "projectile";

    /// <summary>
    /// How much of a buffer is there between the stun duration and when stuns can be re-applied.
    /// </summary>
    private static readonly TimeSpan StamCritBufferTime = TimeSpan.FromSeconds(3f);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StaminaComponent, EntityUnpausedEvent>(OnStamUnpaused);
        SubscribeLocalEvent<StaminaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StaminaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StaminaComponent, ComponentGetState>(OnStamGetState);
        SubscribeLocalEvent<StaminaComponent, ComponentHandleState>(OnStamHandleState);
        SubscribeLocalEvent<StaminaComponent, DisarmedEvent>(OnDisarmed);
        SubscribeLocalEvent<StaminaComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<StaminaDamageOnCollideComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<StaminaDamageOnHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnStamUnpaused(EntityUid uid, StaminaComponent component, ref EntityUnpausedEvent args)
    {
        component.NextUpdate += args.PausedTime;
    }

    private void OnStamGetState(EntityUid uid, StaminaComponent component, ref ComponentGetState args)
    {
        args.State = new StaminaComponentState()
        {
            Critical = component.Critical,
            Decay = component.Decay,
            CritThreshold = component.CritThreshold,
            DecayCooldown = component.DecayCooldown,
            LastUpdate = component.NextUpdate,
            StaminaDamage = component.StaminaDamage,
        };
    }

    private void OnStamHandleState(EntityUid uid, StaminaComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StaminaComponentState state)
            return;

        component.Critical = state.Critical;
        component.Decay = state.Decay;
        component.CritThreshold = state.CritThreshold;
        component.DecayCooldown = state.DecayCooldown;
        component.NextUpdate = state.LastUpdate;
        component.StaminaDamage = state.StaminaDamage;

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

        SetStaminaAlert(uid);
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
        return MathF.Max(0f, component.StaminaDamage - MathF.Max(0f, (float) (curTime - (component.NextUpdate + pauseTime)).TotalSeconds * component.Decay));
    }

    private void OnRejuvenate(EntityUid uid, StaminaComponent component, RejuvenateEvent args)
    {
        if (component.StaminaDamage >= component.CritThreshold)
        {
            ExitStamCrit(uid, component);
        }

        component.StaminaDamage = 0;
        RemComp<ActiveStaminaComponent>(uid);
        Dirty(component);
    }

    private void OnDisarmed(EntityUid uid, StaminaComponent component, DisarmedEvent args)
    {
        if (args.Handled || !_random.Prob(args.PushProbability))
            return;

        if (component.Critical)
            return;

        var damage = args.PushProbability * component.CritThreshold;
        TakeStaminaDamage(uid, damage, component, source:args.Source);

        // We need a better method of getting if the entity is going to resist stam damage, both this and the lines in the foreach at the end of OnHit() are awful
        if (!component.Critical)
            return;

        var targetEnt = Identity.Entity(args.Target, EntityManager);
        var sourceEnt = Identity.Entity(args.Source, EntityManager);

        _popup.PopupEntity(Loc.GetString("stunned-component-disarm-success-others", ("source", sourceEnt), ("target", targetEnt)), targetEnt, Filter.PvsExcept(args.Source), true, PopupType.LargeCaution);
        _popup.PopupCursor(Loc.GetString("stunned-component-disarm-success", ("target", targetEnt)), args.Source, PopupType.Large);

        _adminLogger.Add(LogType.DisarmedKnockdown, LogImpact.Medium, $"{ToPrettyString(args.Source):user} knocked down {ToPrettyString(args.Target):target}");

        args.Handled = true;
    }

    private void OnHit(EntityUid uid, StaminaDamageOnHitComponent component, MeleeHitEvent args)
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

        args.HitSoundOverride = ev.HitSoundOverride;
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
            var oldDamage = comp.StaminaDamage;
            TakeStaminaDamage(ent, damage / toHit.Count, comp, source:args.User, with:args.Weapon);
            if (comp.StaminaDamage.Equals(oldDamage))
            {
                _popup.PopupEntity(Loc.GetString("stamina-resist"), ent, args.User);
            }
        }
    }

    private void OnCollide(EntityUid uid, StaminaDamageOnCollideComponent component, ref StartCollideEvent args)
    {
        if (!args.OurFixture.ID.Equals(CollideFixture)) return;

        TakeStaminaDamage(args.OtherEntity, component.Damage, source:args.OurEntity);
    }

    private void SetStaminaAlert(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
        {
            _alerts.ClearAlert(uid, AlertType.Stamina);
            return;
        }

        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.CritThreshold - component.StaminaDamage), component.CritThreshold, 7);
        _alerts.ShowAlert(uid, AlertType.Stamina, (short) severity);
    }

    public void TakeStaminaDamage(EntityUid uid, float value, StaminaComponent? component = null, EntityUid? source = null, EntityUid? with = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var ev = new BeforeStaminaDamageEvent(value);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        // Have we already reached the point of max stamina damage?
        if (component.Critical)
            return;

        var oldDamage = component.StaminaDamage;
        component.StaminaDamage = MathF.Max(0f, component.StaminaDamage + value);

        // Reset the decay cooldown upon taking damage.
        if (oldDamage < component.StaminaDamage)
        {
            var nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(component.DecayCooldown);

            if (component.NextUpdate < nextUpdate)
                component.NextUpdate = nextUpdate;
        }

        var slowdownThreshold = component.CritThreshold / 2f;

        // If we go above n% then apply slowdown
        if (oldDamage < slowdownThreshold &&
            component.StaminaDamage > slowdownThreshold)
        {
            _stunSystem.TrySlowdown(uid, TimeSpan.FromSeconds(3), true, 0.8f, 0.8f);
        }

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
        Dirty(component);

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
    }

    public override void Update(float frameTime)
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
            Dirty(comp);
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

        var stunTime = TimeSpan.FromSeconds(6);
        _stunSystem.TryParalyze(uid, stunTime, true);

        // Give them buffer before being able to be re-stunned
        component.NextUpdate = _timing.CurTime + stunTime + StamCritBufferTime;
        EnsureComp<ActiveStaminaComponent>(uid);
        Dirty(component);
        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} entered stamina crit");
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
        component.NextUpdate = _timing.CurTime;
        SetStaminaAlert(uid, component);
        RemComp<ActiveStaminaComponent>(uid);
        Dirty(component);
        _adminLogger.Add(LogType.Stamina, LogImpact.Low, $"{ToPrettyString(uid):user} recovered from stamina crit");
    }

    [Serializable, NetSerializable]
    private sealed class StaminaComponentState : ComponentState
    {
        public bool Critical;
        public float Decay;
        public float DecayCooldown;
        public float StaminaDamage;
        public float CritThreshold;
        public TimeSpan LastUpdate;
    }

}

/// <summary>
///     Raised before stamina damage is dealt to allow other systems to cancel it.
/// </summary>
[ByRefEvent]
public record struct BeforeStaminaDamageEvent(float Value, bool Cancelled=false);
