using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared._Starlight.Actions.Stasis;
using Robust.Shared.Player;

namespace Content.Server._Starlight.Actions.Stasis;

public sealed class StasisSystem : SharedStasisSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StasisComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        // If the entity has a stasis component, and the new mob state is dead, exit stasis.
        if (TryComp<StasisComponent>(args.Target, out var comp))
        {
            if (args.NewMobState == MobState.Dead && comp.IsInStasis)
            {
                RaiseLocalEvent(args.Target, new ExitStasisActionEvent());
            }
        }
    }

    protected override void OnMapInit(EntityUid uid, StasisComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
    }

    /// <summary>
    /// Takeths away the action to preform stasis from the entity.
    /// </summary>
    protected override void OnCompRemove(EntityUid uid, StasisComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
    }

    protected override void OnPrepareStasisStart(EntityUid uid, StasisComponent comp,
        PrepareStasisActionEvent args)
    {
        Dirty(uid, comp);

        EnsureComp<StasisFrozenComponent>(uid);

        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.ExitStasisActionEntity, comp.ExitStasisAction);
        _actionsSystem.SetCooldown(comp.ExitStasisActionEntity,
            TimeSpan.FromSeconds(comp.StasisEnterEffectLifetime + 1));

        // Send animation event to all clients
        var ev = new StasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates),
            StasisAnimationType.Prepare);
        RaiseNetworkEvent(ev, Filter.Pvs(uid, entityManager: EntityManager));

        // Schedule the enter stasis event after delay
        Timer.Spawn(TimeSpan.FromSeconds(comp.StasisEnterEffectLifetime), () =>
        {
            if (!TryComp<StasisComponent>(uid, out var stasisComp))
                return;

            var enterEv = new EnterStasisActionEvent();
            RaiseLocalEvent(uid, enterEv);
        });
    }

    protected override void OnEnterStasisStart(EntityUid uid, StasisComponent comp,
        EnterStasisActionEvent args)
    {
        comp.IsInStasis = true;
        comp.IsVisible = false; // Entity becomes invisible when entering stasis to better show the effect

        Dirty(uid, comp);

        // Remove bleeding when entering stasis
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            _bloodstreamSystem.TryModifyBleedAmount(uid, -bloodstream.BleedAmount, bloodstream);
        }

        // Send animation event to all clients
        var ev = new StasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates),
            StasisAnimationType.Enter);
        RaiseNetworkEvent(ev, Filter.Pvs(uid, entityManager: EntityManager));
    }

    protected override void OnExitStasisStart(EntityUid uid, StasisComponent comp, ExitStasisActionEvent args)
    {
        comp.IsInStasis = false;
        comp.IsVisible = true; // Entity becomes visible when exiting stasis

        Dirty(uid, comp);

        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
        _actionsSystem.SetCooldown(comp.EnterStasisActionEntity, TimeSpan.FromSeconds(comp.StasisCooldown));

        RemComp<StasisFrozenComponent>(uid);

        // Send animation event to all clients
        var ev = new StasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates),
            StasisAnimationType.Exit);
        RaiseNetworkEvent(ev, Filter.Pvs(uid, entityManager: EntityManager));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StasisComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (TryComp<MobStateComponent>(uid, out var mobState))
            {
                var currentState = mobState.CurrentState;
                var healingValues = GetHealingValues(currentState, comp);
                OnStasisUpdate(uid, comp, new FrameEventArgs(frameTime), healingValues);
            }
        }
    }

    private void OnDamageChanged(EntityUid uid, StasisComponent component, DamageChangedEvent args)
    {
        // If the entity has a mob state component, and the damage changed event is not healing, apply the resistance.
        if (TryComp<MobStateComponent>(uid, out var mobState))
        {
            var currentState = mobState.CurrentState;
            var healingValues = GetHealingValues(currentState, component);
            ApplyResistance(uid, args, component, healingValues);
        }
    }

    private void ApplyResistance(EntityUid uid, DamageChangedEvent args, StasisComponent comp,
        StasisHealingValues healingValues)
    {
        // Skip if this is healing or if the damage change is from our own healing
        if (!args.DamageIncreased || args.DamageDelta == null || args.Origin == uid)
            return;

        // Only apply resistance if in stasis
        if (!comp.IsInStasis)
            return;

        // Skip if this damage was already modified by stasis
        if (args.Origin == uid && args.DamageDelta.DamageDict.All(x => x.Value < 0))
            return;

        // Create new DamageSpecifier with reduced damage
        var damageToApply = new DamageSpecifier();
        foreach (var (type, amount) in args.DamageDelta.DamageDict)
        {
            damageToApply.DamageDict.Add(type, amount - amount * healingValues.AdditionalDamageResistance);
        }

        // Apply the reduced damage
        _damageableSystem.TryChangeDamage(uid, damageToApply, true, origin: uid);
    }

    private void OnStasisUpdate(EntityUid uid, StasisComponent comp, FrameEventArgs args,
        StasisHealingValues healingValues)
    {
        if (!comp.IsInStasis)
            return;

        // Apply healing effect
        var healAmount = new DamageSpecifier();
        healAmount.DamageDict.Add("Blunt", FixedPoint2.New(healingValues.BluntHeal * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Slash", FixedPoint2.New(healingValues.SlashHeal * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Piercing",
            FixedPoint2.New(healingValues.PiercingHeal * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Heat", FixedPoint2.New(healingValues.HeatHeal * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Cold", FixedPoint2.New(healingValues.ColdHeal * args.DeltaSeconds * -1));

        if (TryComp<DamageableComponent>(uid, out _))
        {
            _damageableSystem.TryChangeDamage(uid, healAmount, true, origin: uid);
        }

        // Heal bleeding
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream) && bloodstream.BleedAmount > 0)
        {
            _bloodstreamSystem.TryModifyBleedAmount(uid, -healingValues.BleedHeal * (float)args.DeltaSeconds,
                bloodstream);
        }
    }

    /// <summary>
    /// Gets the healing values for the stasis effect based on the mob state.
    /// </summary>
    private StasisHealingValues GetHealingValues(MobState state, StasisComponent comp)
    {
        return state switch
        {
            MobState.Alive => new StasisHealingValues(comp.StasisBluntHealPerSecond, comp.StasisSlashingHealPerSecond,
                comp.StasisPiercingHealPerSecond, comp.StasisHeatHealPerSecond, comp.StasisColdHealPerSecond,
                comp.StasisBleedHealPerSecond, comp.StasisAdditionalDamageResistance),
            MobState.Critical => new StasisHealingValues(comp.StasisInCritBluntHealPerSecond,
                comp.StasisInCritSlashingHealPerSecond, comp.StasisInCritPiercingHealPerSecond,
                comp.StasisInCritHeatHealPerSecond, comp.StasisInCritColdHealPerSecond,
                comp.StasisInCritBleedHealPerSecond, comp.StasisInCritAdditionalDamageResistance),
            MobState.Invalid => new StasisHealingValues(0, 0, 0, 0, 0, 0, 0),
            MobState.Dead => new StasisHealingValues(0, 0, 0, 0, 0, 0, 0),
            _ => new StasisHealingValues(0, 0, 0, 0, 0, 0, 0),
        };
    }
}

/// <summary>
/// A struct that contains the healing values for the stasis effect.
/// </summary>
sealed class StasisHealingValues(
    float bluntHeal,
    float slashHeal,
    float piercingHeal,
    float heatHeal,
    float coldHeal,
    float bleedHeal,
    float additionalDamageResistance)
{
    public float BluntHeal { get; private set; } = bluntHeal;
    public float SlashHeal { get; private set; } = slashHeal;
    public float PiercingHeal { get; private set; } = piercingHeal;
    public float HeatHeal { get; private set; } = heatHeal;
    public float ColdHeal { get; private set; } = coldHeal;
    public float AdditionalDamageResistance { get; private set; } = additionalDamageResistance;
    public float BleedHeal { get; private set; } = bleedHeal;
}
