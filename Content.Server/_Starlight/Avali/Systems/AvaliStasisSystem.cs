using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Starlight.Avali.Components;
using Content.Shared.Starlight.Avali.Events;
using Content.Shared.Starlight.Avali.Systems;

namespace Content.Server._Starlight.Avali.Systems;

public sealed class AvaliStasisSystem : SharedAvaliStasisSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AvaliStasisComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        // If the entity has a stasis component, and the new mob state is dead, exit stasis.
        if (TryComp<AvaliStasisComponent>(args.Target, out var comp))
        {
            if (args.NewMobState == MobState.Dead && comp.IsInStasis)
            {
                RaiseLocalEvent(args.Target, new AvaliExitStasisActionEvent());
            }
        }
    }

    protected override void OnMapInit(EntityUid uid, AvaliStasisComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
    }

    /// <summary>
    /// Takeths away the action to preform stasis from the entity.
    /// </summary>
    protected override void OnCompRemove(EntityUid uid, AvaliStasisComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
    }

    protected override void OnPrepareStasisStart(EntityUid uid, AvaliStasisComponent comp,
        AvaliPrepareStasisActionEvent args)
    {
        Dirty(uid, comp);
        
        EnsureComp<AvaliStasisFrozenComponent>(uid);
        
        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.ExitStasisActionEntity, comp.ExitStasisAction);
        _actionsSystem.SetCooldown(comp.ExitStasisActionEntity, TimeSpan.FromSeconds(comp.StasisEnterEffectLifetime + 1));

        // Send animation event to all clients
        var ev = new AvaliStasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates),
            AvaliStasisAnimationType.Prepare);
        RaiseNetworkEvent(ev);

        // Schedule the enter stasis event after delay
        Timer.Spawn(TimeSpan.FromSeconds(comp.StasisEnterEffectLifetime), () =>
        {
            if (!TryComp<AvaliStasisComponent>(uid, out var stasisComp))
                return;

            var enterEv = new AvaliEnterStasisActionEvent();
            RaiseLocalEvent(uid, enterEv);
        });
    }
    
    protected override void OnEnterStasisStart(EntityUid uid, AvaliStasisComponent comp,
        AvaliEnterStasisActionEvent args)
    {
        comp.IsInStasis = true;

        Dirty(uid, comp);

        // Remove bleeding when entering stasis
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            _bloodstreamSystem.TryModifyBleedAmount(uid, -bloodstream.BleedAmount, bloodstream);
        }

        // Send animation event to all clients
        var ev = new AvaliStasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates),
            AvaliStasisAnimationType.Enter);
        RaiseNetworkEvent(ev);
    }

    protected override void OnExitStasisStart(EntityUid uid, AvaliStasisComponent comp, AvaliExitStasisActionEvent args)
    {
        comp.IsInStasis = false;
        Dirty(uid, comp);

        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
        _actionsSystem.SetCooldown(comp.EnterStasisActionEntity, TimeSpan.FromSeconds(comp.StasisCooldown));

        RemComp<AvaliStasisFrozenComponent>(uid);

        // Send animation event to all clients
        var ev = new AvaliStasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates),
            AvaliStasisAnimationType.Exit);
        RaiseNetworkEvent(ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AvaliStasisComponent>();
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

    private void OnDamageChanged(EntityUid uid, AvaliStasisComponent component, DamageChangedEvent args)
    {
            // If the entity has a mob state component, and the damage changed event is not healing, apply the resistance.
            if (TryComp<MobStateComponent>(uid, out var mobState))
            {
                var currentState = mobState.CurrentState;
                var healingValues = GetHealingValues(currentState, component);
                ApplyResistance(uid, args, component, healingValues);
            }
    }
    
    private void ApplyResistance(EntityUid uid, DamageChangedEvent args, AvaliStasisComponent comp, AvaliStasisHealingValues healingValues) {
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

    private void OnStasisUpdate(EntityUid uid, AvaliStasisComponent comp, FrameEventArgs args,
        AvaliStasisHealingValues healingValues)
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
    private AvaliStasisHealingValues GetHealingValues(MobState state, AvaliStasisComponent comp)
    {
        return state switch
        {
            MobState.Alive => new AvaliStasisHealingValues(comp.StasisBluntHealPerSecond, comp.StasisSlashingHealPerSecond, comp.StasisPiercingHealPerSecond, comp.StasisHeatHealPerSecond, comp.StasisColdHealPerSecond, comp.StasisBleedHealPerSecond, comp.StasisAdditionalDamageResistance),
            MobState.Critical => new AvaliStasisHealingValues(comp.StasisInCritBluntHealPerSecond, comp.StasisInCritSlashingHealPerSecond, comp.StasisInCritPiercingHealPerSecond, comp.StasisInCritHeatHealPerSecond, comp.StasisInCritColdHealPerSecond, comp.StasisInCritBleedHealPerSecond, comp.StasisInCritAdditionalDamageResistance),
            MobState.Invalid =>  new AvaliStasisHealingValues(0, 0, 0, 0, 0, 0, 0),
            MobState.Dead => new AvaliStasisHealingValues(0, 0, 0, 0, 0, 0, 0),
            _ => new AvaliStasisHealingValues(0, 0, 0, 0, 0, 0, 0),
        };
    }
}

/// <summary>
/// A struct that contains the healing values for the stasis effect.
/// </summary>
sealed class AvaliStasisHealingValues(
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