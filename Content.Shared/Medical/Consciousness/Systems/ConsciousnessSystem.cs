using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Consciousness.Systems;

public sealed class ConsciousnessSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ConsciousnessComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ConsciousnessComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<ConsciousnessComponent, ComponentHandleState>(OnComponentHandleState);
    }

    private void OnComponentGetState(EntityUid uid, ConsciousnessComponent component, ref ComponentGetState args)
    {

    }

    private void OnComponentHandleState(EntityUid uid, ConsciousnessComponent component, ref ComponentHandleState args)
    {

    }

    private void OnComponentStartup(EntityUid uid, ConsciousnessComponent component, ComponentStartup args)
    {
        CheckConsciousness(uid, component);
    }



    public FixedPoint2 GetConsciousness(EntityUid entity,
        ConsciousnessComponent? consciousness = null)
    {
        return !Resolve(entity, ref consciousness)
            ? FixedPoint2.Zero
            : FixedPoint2.Min(consciousness.Cap, consciousness.Base * consciousness.Modifier + consciousness.Offset);
    }

    public bool IsConscious(EntityUid entity, out FixedPoint2 consciousnessValue,
        ConsciousnessComponent? consciousness = null)
    {
        consciousnessValue = GetConsciousness(entity, consciousness);
        if (!Resolve(entity, ref consciousness))
            return true;
        return consciousnessValue > consciousness.PassOutThreshold;
    }

    public void UpdateBaseConsciousness(EntityUid entity, FixedPoint2 baseValue,
        ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(entity, ref consciousness))
            return;
        var ev = new UpdateBaseConsciousnessEvent(baseValue);
        RaiseLocalEvent(entity, ref ev);
        consciousness.Base = ev.Base;
        CheckConsciousness(entity, consciousness);
    }

    public void UpdateConsciousnessModifier(EntityUid entity, FixedPoint2 modifier,
        ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(entity, ref consciousness))
            return;
        var ev = new UpdateConsciousnessModifierEvent(modifier);
        RaiseLocalEvent(entity, ref ev);
        consciousness.Modifier = ev.Modifier;
        CheckConsciousness(entity, consciousness);
    }

    public void UpdateConsciousnessOffset(EntityUid entity, FixedPoint2 offset,
        ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(entity, ref consciousness))
            return;
        var ev = new UpdateConsciousnessOffsetEvent(offset);
        RaiseLocalEvent(entity, ref ev);
        consciousness.Offset = ev.Offset;
        CheckConsciousness(entity, consciousness);
    }

    public void UpdateConsciousnessCap(EntityUid entity, FixedPoint2 cap,
        ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(entity, ref consciousness))
            return;
        var ev = new UpdateConsciousnessCapEvent(cap);
        RaiseLocalEvent(entity, ref ev);
        consciousness.Cap = ev.Cap;
        CheckConsciousness(entity, consciousness);
    }

    private void CheckConsciousness(EntityUid entity, ConsciousnessComponent consciousness)
    {
        var ev = new ConsciousnessUpdateEvent(
            IsConscious(entity, out var consciousnessValue , consciousness),
            consciousnessValue);
        RaiseLocalEvent(entity, ev, true);
    }
}
