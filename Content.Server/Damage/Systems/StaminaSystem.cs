using Content.Server.Damage.Components;
using Content.Server.Damage.Events;
using Content.Server.Weapon.Melee;
using Content.Shared.Stunnable;
using Robust.Shared.Collections;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;

namespace Content.Server.Damage.Systems;

public sealed class StaminaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    /// <summary>
    /// How long after receiving stamina damage before it is allowed to decay.
    /// </summary>
    private const float DecayCooldown = 3f;

    private const float UpdateCooldown = 2f;
    private float _accumulator = 0f;

    private const string CollideFixture = "projectile";

    /// <summary>
    /// How much stamina damage is applied above cap.
    /// </summary>
    private const float CritExcess = 18f;

    private readonly List<EntityUid> _dirtyEntities = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StaminaDamageOnCollideComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<StaminaDamageOnHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(EntityUid uid, StaminaDamageOnHitComponent component, MeleeHitEvent args)
    {
        if (component.Damage <= 0f) return;

        var ev = new StaminaDamageOnHitAttemptEvent();
        RaiseLocalEvent(uid, ref ev);

        if (ev.Cancelled) return;

        var stamQuery = GetEntityQuery<StaminaComponent>();
        var toHit = new ValueList<StaminaComponent>();

        // Split stamina damage between all eligible targets.
        foreach (var ent in args.HitEntities)
        {
            if (!stamQuery.TryGetComponent(ent, out var stam)) continue;
            toHit.Add(stam);
        }

        foreach (var comp in toHit)
        {
            TakeStaminaDamage(comp.Owner, component.Damage / toHit.Count, comp);
        }
    }

    private void OnCollide(EntityUid uid, StaminaDamageOnCollideComponent component, StartCollideEvent args)
    {
        if (!args.OurFixture.ID.Equals(CollideFixture)) return;

        TakeStaminaDamage(args.OtherFixture.Body.Owner, component.Damage);
    }

    public void TakeStaminaDamage(EntityUid uid, float value, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Critical) return;

        var oldDamage = component.StaminaDamage;
        component.StaminaDamage = MathF.Max(0f, component.StaminaDamage + value);

        // Reset the decay cooldown upon taking damage.
        if (oldDamage < component.StaminaDamage)
        {
            component.StaminaDecayAccumulator = DecayCooldown;
        }

        // Can't do it here as resetting prediction gets cooked.
        _dirtyEntities.Add(uid);

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
    }

    /// <summary>
    /// Estimate how long it will take to get out of stamina crit.
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    private TimeSpan GetStamCritTime(StaminaComponent component)
    {
        var damageableSeconds = MathF.Max(0f, (component.StaminaDamage - component.CritThreshold) / component.Decay);
        return TimeSpan.FromSeconds(damageableSeconds);
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

        component.Critical = true;
        var stamDamageCap = component.CritThreshold + CritExcess;
        component.StaminaDamage = stamDamageCap;
        component.StaminaDecayAccumulator = 0f;

        var stunTime = GetStamCritTime(component);
        _stunSystem.TryParalyze(uid, stunTime, true);

        component.StaminaDecayAccumulator = (float) stunTime.TotalSeconds;
    }

    private void ExitStamCrit(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            !component.Critical) return;

        component.Critical = false;
        component.StaminaDamage = 0f;
    }
}
