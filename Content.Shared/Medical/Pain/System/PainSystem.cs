using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
using Content.Shared.Medical.Consciousness.Systems;
using Content.Shared.Medical.Pain.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Pain.System;

public sealed class PainSystem : EntitySystem
{
    [Dependency] private ConsciousnessSystem _consciousnessSystem = default!;

    //TODO: Make this a CVAR
    private readonly FixedPoint2 _consciousnessPainEffectMax = 40;
    private readonly FixedPoint2 _consciousnessPainFloor = 0.1;

    public override void Initialize()
    {
        SubscribeLocalEvent<PainReceiverComponent, ComponentGetState>(OnPainThresholdGetState);
        SubscribeLocalEvent<PainReceiverComponent, ComponentHandleState>(OnPainThresholdHandleState);

        SubscribeLocalEvent<PainInflicterComponent, ComponentGetState>(OnPainInflicterGetState);
        SubscribeLocalEvent<PainInflicterComponent, ComponentHandleState>(OnPainInflicterHandleState);
    }

    private void OnPainInflicterHandleState(EntityUid uid, PainInflicterComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not PainInflicterComponentState state)
            return;
        component.Pain = state.Pain;
    }

    private void OnPainInflicterGetState(EntityUid uid, PainInflicterComponent component, ref ComponentGetState args)
    {
        args.State = new PainInflicterComponentState(
            component.Pain
        );
    }

    private void OnPainThresholdHandleState(EntityUid uid, PainReceiverComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not PainReceiverComponentState state)
            return;
        component.MaxPain = state.MaxPain;
        component.PainModifier = state.PainModifier;
        component.RawPain = state.RawPain;
        var consciousnessDelta = state.ConsciousnessDamage - component.ConsciousnessDamage;
        component.ConsciousnessDamage = state.ConsciousnessDamage;
        if (consciousnessDelta != 0)
        {
            _consciousnessSystem.AddToDamage(uid, consciousnessDelta);
        }
    }

    private void OnPainThresholdGetState(EntityUid uid, PainReceiverComponent component, ref ComponentGetState args)
    {
        args.State = new PainReceiverComponentState(
            component.PainModifier,
            component.RawPain,
            component.MaxPain,
            component.ConsciousnessDamage);
    }


    public FixedPoint2 GetPain(EntityUid target, PainReceiverComponent? painThresholds = null)
    {
        if (!Resolve(target, ref painThresholds))
            return -1;
        return painThresholds.RawPain * painThresholds.PainModifier;
    }

    //Add or remove pain from a pain receiver
    public bool ApplyPain(EntityUid target, FixedPoint2 pain, PainReceiverComponent? painReceiver = null,
        EntityUid? inflicterEntity = null)
    {
        if (pain == 0 || !Resolve(target, ref painReceiver))
            return false;

        var ev = new InflictPainEvent(painReceiver, pain, inflicterEntity);
        RaiseLocalEvent(target, ref ev, true);
        if (ev.Canceled)
            return false;
        Dirty(target);
        ApplyPainEffects(target, painReceiver, pain);
        return true;
    }

    private FixedPoint2 CalculateNewConsciousnessEffect(PainReceiverComponent painReceiver, FixedPoint2 pain)
    {
        //todo: may need to multiply pain by the painmod
        //We do a lil' bit of mathin
        // https://www.desmos.com/calculator/kxtd0njdhn
        var painFloor = _consciousnessPainFloor * painReceiver.MaxPain;
        var adjPainMod = painReceiver.PainModifier / (painReceiver.MaxPain - painFloor);
        return FixedPoint2.Clamp(
            FixedPoint2.Max(0, (pain - painFloor) * adjPainMod) *
            _consciousnessPainEffectMax, 0, _consciousnessPainEffectMax);
    }

    private void ApplyPainEffects(EntityUid target, PainReceiverComponent? painReceiver, FixedPoint2 addedPain)
    {
        if (!Resolve(target, ref painReceiver))
            return;
        var previousConsciousnessEffect = painReceiver.ConsciousnessDamage;
        painReceiver.RawPain += addedPain;
        painReceiver.ConsciousnessDamage = CalculateNewConsciousnessEffect(painReceiver, painReceiver.Pain);
        var consciousnessDelta = painReceiver.ConsciousnessDamage -
                                 previousConsciousnessEffect;
        if (consciousnessDelta == 0)
            return;
        _consciousnessSystem.AddToDamage(target, consciousnessDelta);
    }
}
