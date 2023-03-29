using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Consciousness.Systems;

public sealed class ConsciousnessSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsciousnessComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ConsciousnessComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<ConsciousnessComponent, ComponentHandleState>(OnComponentHandleState);
        SubscribeLocalEvent<ConsciousnessComponent, UpdateMobStateEvent>(OnUpdateMobState);
    }

    private void OnUpdateMobState(EntityUid uid, ConsciousnessComponent component, ref UpdateMobStateEvent args)
    {
        if (!IsConscious(uid, out _, component))
        {
            if (args.Component.CurrentState == MobState.Alive)
            {
                args.State = MobState.Critical;
            }

            return;
        }

        args.State = MobState.Alive;
    }

    private void OnComponentGetState(EntityUid uid, ConsciousnessComponent component, ref ComponentGetState args)
    {
        args.State = new ConsciousnessComponentState(
            component.Threshold,
            component.Damage,
            component.Modifier,
            component.Clamp,
            component.Capacity
        );
    }

    private void OnComponentHandleState(EntityUid uid, ConsciousnessComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ConsciousnessComponentState state)
            return;
        component.Capacity = state.Capacity;
        component.Threshold = state.Threshold;
        component.Damage = state.Damage;
        component.Modifier = state.Modifier;
        component.Clamp = state.Clamp;
        CheckConsciousness(uid, component);
    }

    private void OnComponentStartup(EntityUid uid, ConsciousnessComponent component, ComponentStartup args)
    {
        CheckConsciousness(uid, component);
    }

    public FixedPoint2 GetConsciousness(EntityUid entity, ConsciousnessComponent? consciousness = null)
    {
        return !Resolve(entity, ref consciousness)
            ? FixedPoint2.Zero
            : FixedPoint2.Min(consciousness.Clamp,
                consciousness.Capacity - consciousness.Damage * consciousness.Modifier);
    }

    public bool IsConscious(EntityUid entity, out FixedPoint2 consciousnessValue,
        ConsciousnessComponent? consciousness = null)
    {
        consciousnessValue = 0;
        if (!Resolve(entity, ref consciousness))
            return true;
        consciousnessValue = GetConsciousness(entity, consciousness);
        return consciousnessValue > consciousness.Threshold;
    }

    private void CheckConsciousness(EntityUid entity, ConsciousnessComponent consciousness)
    {
        var isConscious = IsConscious(entity, out var consciousnessValue, consciousness);
        var ev = new ConsciousnessUpdatedEvent(isConscious, consciousnessValue);
        RaiseLocalEvent(entity, ref ev, true);
        _mobStateSystem.UpdateMobState(entity);
    }

    public bool AddToThreshold(EntityUid entity, FixedPoint2 threshold, ConsciousnessComponent? consciousness = null)
    {
        if (threshold == 0 || !Resolve(entity, ref consciousness))
            return false;
        var ev = new UpdateConsciousnessThresholdEvent()
        {
            Component = consciousness,
            Threshold = consciousness.Threshold + threshold
        };
        RaiseLocalEvent(entity, ref ev);
        if (ev.Canceled)
            return true;
        consciousness.Threshold = FixedPoint2.Clamp(ev.Threshold, 0, consciousness.Capacity);
        CheckConsciousness(entity, consciousness);
        return true;
        Dirty(entity);
    }

    public bool SetThreshold(EntityUid entity, FixedPoint2 newThreshold, ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(entity, ref consciousness))
            return false;
        var ev = new UpdateConsciousnessThresholdEvent()
        {
            Component = consciousness,
            Threshold = newThreshold
        };
        RaiseLocalEvent(entity, ref ev);
        if (ev.Canceled)
            return true;
        consciousness.Threshold = FixedPoint2.Clamp(ev.Threshold, 0, consciousness.Capacity);
        CheckConsciousness(entity, consciousness);
        Dirty(entity);
        return true;
    }

    public bool AddToClamp(EntityUid entity, FixedPoint2 clamp, ConsciousnessComponent? consciousness = null)
    {
        if (clamp == 0 ||  !Resolve(entity, ref consciousness))
            return false;
        var ev = new UpdateConsciousnessClampEvent()
        {
            Component = consciousness,
            Clamp = consciousness.Clamp + clamp
        };
        RaiseLocalEvent(entity, ref ev);
        if (ev.Canceled)
            return true;
        consciousness.Clamp = FixedPoint2.Clamp(ev.Clamp, 0, consciousness.Capacity);
        CheckConsciousness(entity, consciousness);
        Dirty(entity);
        return true;
    }

    public bool SetClamp(EntityUid entity, FixedPoint2 newClamp, ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(entity, ref consciousness))
            return false;
        var ev = new UpdateConsciousnessClampEvent()
        {
            Component = consciousness,
            Clamp = newClamp
        };
        RaiseLocalEvent(entity, ref ev);
        if (ev.Canceled)
            return true;
        consciousness.Clamp = FixedPoint2.Clamp(ev.Clamp, 0, consciousness.Capacity);
        CheckConsciousness(entity, consciousness);
        Dirty(entity);
        return true;
    }

    public bool AddToDamage(EntityUid entity, FixedPoint2 damage, ConsciousnessComponent? consciousness = null)
    {
        if (damage == 0 || !Resolve(entity, ref consciousness))
            return false;
        var ev = new UpdateConsciousnessDamageEvent()
        {
            Component = consciousness,
            Damage = consciousness.Damage + damage
        };
        RaiseLocalEvent(entity, ref ev);
        if (ev.Canceled)
            return true;
        consciousness.Damage = FixedPoint2.Max(ev.Damage, 0);
        CheckConsciousness(entity, consciousness);
        Dirty(entity);
        return true;
    }

    public bool SetDamage(EntityUid entity, FixedPoint2 newDamage, ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(entity, ref consciousness))
            return false;
        var ev = new UpdateConsciousnessDamageEvent()
        {
            Component = consciousness,
            Damage = newDamage
        };
        RaiseLocalEvent(entity, ref ev);
        if (ev.Canceled)
            return true;
        consciousness.Damage = FixedPoint2.Max(ev.Damage, 0);
        CheckConsciousness(entity, consciousness);
        Dirty(entity);
        return true;
    }

    public bool AddToModifier(EntityUid entity, FixedPoint2 modifier, ConsciousnessComponent? consciousness = null)
    {
        if (modifier == 0 || !Resolve(entity, ref consciousness))
            return false;
        var ev = new UpdateConsciousnessModifierEvent()
        {
            Component = consciousness,
            Modifier = consciousness.Modifier + modifier
        };
        RaiseLocalEvent(entity, ref ev);
        if (ev.Canceled)
            return true;
        consciousness.Modifier = FixedPoint2.Max(ev.Modifier, 0);
        CheckConsciousness(entity, consciousness);
        Dirty(entity);
        return true;
    }

    public bool SetModifier(EntityUid entity, FixedPoint2 modifier, ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(entity, ref consciousness))
            return false;
        var ev = new UpdateConsciousnessModifierEvent()
        {
            Component = consciousness,
            Modifier = modifier
        };
        RaiseLocalEvent(entity, ref ev);
        if (ev.Canceled)
            return true;
        consciousness.Modifier = FixedPoint2.Max(ev.Modifier, 0);
        CheckConsciousness(entity, consciousness);
        Dirty(entity);
        return true;
    }

}
