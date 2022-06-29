using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Content.Shared.Stunnable;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Damage;

public sealed class StaminaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    public const string StaminaDamageType = "Stamina";

    /// <summary>
    /// How long after receiving stamina damage before it is allowed to decay.
    /// </summary>
    private const float DecayCooldown = 5f;

    private const float UpdateCooldown = 2f;
    private float _accumulator = 0f;

    /// <summary>
    /// How much stamina damage is applied above cap.
    /// </summary>
    private const float CritExcess = 30f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StaminaComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<StaminaComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<StaminaDamageOnCollide, StartCollideEvent>(OnCollide);
    }

    private void OnGetState(EntityUid uid, StaminaComponent component, ref ComponentGetState args)
    {
        args.State = new StaminaComponentState()
        {
            Damage = component.StaminaDamage,
        };
    }

    private void OnHandleState(EntityUid uid, StaminaComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StaminaComponentState state) return;
        TakeStaminaDamage(uid, state.Damage - component.StaminaDamage, component);
    }

    private void OnCollide(EntityUid uid, StaminaDamageOnCollide component, StartCollideEvent args)
    {
        TakeStaminaDamage(args.OtherFixture.Body.Owner, component.Damage);
    }

    public void TakeStaminaDamage(EntityUid uid, float value, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component, false)) return;

        var oldDamage = component.StaminaDamage;
        component.StaminaDamage = MathF.Max(0f, component.StaminaDamage + value);

        // Reset the decay cooldown upon taking damage.
        if (oldDamage < component.StaminaDamage)
        {
            component.StaminaDecayAccumulator = DecayCooldown;
        }

        if (component.StaminaDamage > 0f)
        {
            EnsureComp<ActiveStaminaComponent>(uid);
        }
        else
        {
            RemComp<ActiveStaminaComponent>(uid);
        }

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

        Dirty(component);
    }

    /// <summary>
    /// Estimate how long it will take to get out of stamina crit.
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    private TimeSpan GetStamCritTime(StaminaComponent component)
    {
        var damageableSeconds = MathF.Min(0f, (component.StaminaDamage - component.CritThreshold) / component.Decay);
        return TimeSpan.FromSeconds(damageableSeconds);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        _accumulator -= frameTime;

        if (_accumulator > 0f) return;

        _accumulator += UpdateCooldown;

        var stamQuery = GetEntityQuery<StaminaComponent>();

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
        RemComp<ActiveStaminaComponent>(uid);
    }

    [Serializable, NetSerializable]
    protected sealed class StaminaComponentState : ComponentState
    {
        public float Damage;
    }
}
