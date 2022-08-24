using Content.Server.Damage.Components;
using Content.Server.Damage.Events;
using Content.Server.Popups;
using Content.Server.Weapon.Melee;
using Content.Shared.Alert;
using Content.Shared.Rounding;
using Content.Shared.Stunnable;
using Robust.Shared.Collections;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Audio;


namespace Content.Server.Damage.Systems;

public sealed class StaminaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    private const float UpdateCooldown = 2f;
    private float _accumulator;

    private const string CollideFixture = "projectile";

    /// <summary>
    /// How much of a buffer is there between the stun duration and when stuns can be re-applied.
    /// </summary>
    private const float StamCritBufferTime = 3f;

    private readonly List<EntityUid> _dirtyEntities = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StaminaDamageOnCollideComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<StaminaDamageOnHitComponent, MeleeHitEvent>(OnHit);
        SubscribeLocalEvent<StaminaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StaminaComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, StaminaComponent component, ComponentShutdown args)
    {
        SetStaminaAlert(uid);
    }

    private void OnStartup(EntityUid uid, StaminaComponent component, ComponentStartup args)
    {
        SetStaminaAlert(uid, component);
    }

    private void OnHit(EntityUid uid, StaminaDamageOnHitComponent component, MeleeHitEvent args)
    {
        if (component.Damage <= 0f) return;

        var ev = new StaminaDamageOnHitAttemptEvent();
        RaiseLocalEvent(uid, ref ev);

        if (ev.Cancelled) return;

        args.HitSoundOverride = ev.HitSoundOverride;
        var stamQuery = GetEntityQuery<StaminaComponent>();
        var toHit = new ValueList<StaminaComponent>();

        // Split stamina damage between all eligible targets.
        foreach (var ent in args.HitEntities)
        {
            if (!stamQuery.TryGetComponent(ent, out var stam)) continue;
            toHit.Add(stam);
        }

        var hitEvent = new StaminaMeleeHitEvent(toHit);
        RaiseLocalEvent(uid, hitEvent, false);

        if (hitEvent.Handled)
            return;

        var damage = component.Damage;

        damage *= hitEvent.Multiplier;

        damage += hitEvent.FlatModifier;

        foreach (var comp in toHit)
        {
            var oldDamage = comp.StaminaDamage;
            TakeStaminaDamage(comp.Owner, damage / toHit.Count, comp, component.KnockdownSound);
            if (comp.StaminaDamage.Equals(oldDamage))
            {
                _popup.PopupEntity(Loc.GetString("stamina-resist"), comp.Owner, Filter.Entities(args.User));
            }
        }
    }

    private void OnCollide(EntityUid uid, StaminaDamageOnCollideComponent component, StartCollideEvent args)
    {
        if (!args.OurFixture.ID.Equals(CollideFixture)) return;

        TakeStaminaDamage(args.OtherFixture.Body.Owner, component.Damage);
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

    public void TakeStaminaDamage(EntityUid uid, float value, StaminaComponent? component = null, SoundSpecifier? knockdownSound = null)
    {
        if (!Resolve(uid, ref component, false) || component.Critical) return;

        var oldDamage = component.StaminaDamage;
        component.StaminaDamage = MathF.Max(0f, component.StaminaDamage + value);

        // Reset the decay cooldown upon taking damage.
        if (oldDamage < component.StaminaDamage)
        {
            component.StaminaDecayAccumulator = component.DecayCooldown;
        }

        var slowdownThreshold = component.CritThreshold / 2f;

        // If we go above n% then apply slowdown
        if (oldDamage < slowdownThreshold &&
            component.StaminaDamage > slowdownThreshold)
        {
            _stunSystem.TrySlowdown(uid, TimeSpan.FromSeconds(3), true, 0.8f, 0.8f);
        }

        SetStaminaAlert(uid, component);

        // Can't do it here as resetting prediction gets cooked.
        _dirtyEntities.Add(uid);

        if (!component.Critical)
        {
            if (component.StaminaDamage >= component.CritThreshold)
            {
                if (knockdownSound != null)
                    SoundSystem.Play(knockdownSound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), uid, knockdownSound.Params);
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
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        _accumulator -= frameTime;

        if (_accumulator > 0f) return;

        var stamQuery = GetEntityQuery<StaminaComponent>();

        foreach (var uid in _dirtyEntities)
        {
            // Don't need to RemComp as they will get handled below.
            if (!stamQuery.TryGetComponent(uid, out var comp) || comp.StaminaDamage <= 0f) continue;
            EnsureComp<ActiveStaminaComponent>(uid);
        }

        _dirtyEntities.Clear();
        _accumulator += UpdateCooldown;

        foreach (var active in EntityQuery<ActiveStaminaComponent>())
        {
            // Just in case we have active but not stamina we'll check and account for it.
            if (!stamQuery.TryGetComponent(active.Owner, out var comp) ||
                comp.StaminaDamage <= 0f)
            {
                RemComp<ActiveStaminaComponent>(active.Owner);
                continue;
            }

            comp.StaminaDecayAccumulator -= UpdateCooldown;

            if (comp.StaminaDecayAccumulator > 0f) continue;

            // We were in crit so come out of it and continue.
            if (comp.Critical)
            {
                ExitStamCrit(active.Owner, comp);
                continue;
            }

            comp.StaminaDecayAccumulator = 0f;
            TakeStaminaDamage(comp.Owner, -comp.Decay * UpdateCooldown, comp);
        }
    }

    private void EnterStamCrit(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            component.Critical) return;

        // To make the difference between a stun and a stamcrit clear
        // TODO: Mask?

        component.Critical = true;
        component.StaminaDamage = component.CritThreshold;
        component.StaminaDecayAccumulator = 0f;

        var stunTime = TimeSpan.FromSeconds(6);
        _stunSystem.TryParalyze(uid, stunTime, true);

        // Give them buffer before being able to be re-stunned
        component.StaminaDecayAccumulator = (float) stunTime.TotalSeconds + StamCritBufferTime;
    }

    private void ExitStamCrit(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            !component.Critical) return;

        component.Critical = false;
        component.StaminaDamage = 0f;
        SetStaminaAlert(uid, component);
    }
}
