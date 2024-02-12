using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
using Content.Shared.Medical.Consciousness.Events;
using Content.Shared.Medical.Organs.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Medical.Consciousness.Systems;

public sealed class ConsciousnessSystem : EntitySystem
{

    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;


    //TODO: fix how consciousness cap/clamping works, do not actually clamp the value, use either consciousnessCap/Consciousness depending on which is lower instead!

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsciousnessComponent,UpdateMobStateEvent>(OnMobstateChanged);
        SubscribeLocalEvent<ConsciousnessComponent,MapInitEvent>(ConsciousnessInit);
        SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>(OnBrainRemoved);
        SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>(OnBrainAdded);
    }

    private void OnBrainAdded(EntityUid uid, BrainComponent component, ref OrganAddedToBodyEvent args)
    {
        if (!TryComp<ConsciousnessComponent>(args.Body, out var consciousness))
            return;
        if (consciousness.LinkedBrain == null)
        {
            consciousness.LinkedBrain = uid;
            Dirty(args.Body, consciousness);
            return;
        }
        consciousness.LinkedBrain = uid;
        AddConsciousnessModifier(
            new Entity<ConsciousnessComponent?, MobStateComponent?>(args.Body, consciousness, null),
            ConsciousnessComponent.MaxConsciousness);
    }

    private void OnBrainRemoved(EntityUid brainUid, BrainComponent brain, ref OrganRemovedFromBodyEvent args)
    {
        if (!TryComp<ConsciousnessComponent>(args.OldBody, out var consciousness) || consciousness.LinkedBrain != brainUid)
            return;
        consciousness.LinkedBrain = EntityUid.Invalid;
        AddConsciousnessModifier(
            new Entity<ConsciousnessComponent?, MobStateComponent?>(args.OldBody, consciousness, null),
            -ConsciousnessComponent.MaxConsciousness);
    }

    private void ConsciousnessInit(EntityUid uid, ConsciousnessComponent consciousness, MapInitEvent args)
    {
        UpdateConsciousness(new Entity<ConsciousnessComponent?, MobStateComponent?>(uid, consciousness, null));
    }

    private void OnMobstateChanged(EntityUid uid, ConsciousnessComponent consciousness, ref UpdateMobStateEvent args)
    {
        //Do nothing if mobstate handling is set to be overriden or if we are conscious
        if (consciousness.OverridenByMobstate || GetConsciousness(consciousness) > 0)
            return;
        args.State = MobState.Dead;
        var ev = new EntityConsciousnessKillEvent(new Entity<ConsciousnessComponent>(uid, consciousness));
        RaiseConsciousnessEvent(uid, ref ev);
    }

    public void AddConsciousnessModifier(Entity<ConsciousnessComponent?, MobStateComponent?> conscious, FixedPoint2 modifierToAdd)
    {
        if (!Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;

        var newMod = modifierToAdd + conscious.Comp1.Modifier;
        var delta = GetConsciousness(conscious) - CalculateNewConsciousness(conscious.Comp1, newConsciousnessModifier: newMod);
        var attemptEv = new ChangeConsciousnessAttemptEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref attemptEv);
        if (attemptEv.Canceled)
            return;

        conscious.Comp1.Modifier = newMod;
        var ev = new ConsciousnessChangedEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref ev);

        UpdateConsciousness(conscious, conscious.Comp1, conscious.Comp2);
        Dirty(conscious, conscious.Comp1);
    }

    public void AddConsciousnessMultiplier(Entity<ConsciousnessComponent?, MobStateComponent?> conscious, FixedPoint2 multiplierToAdd)
    {
        if (!Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        var newMult = multiplierToAdd + conscious.Comp1.Modifier;
        if (newMult < 0) //clamp multiplier to never go below 0
            newMult = 0;
        var delta = GetConsciousness(conscious) - CalculateNewConsciousness(conscious.Comp1, newConsciousnessMultiplier: newMult);
        var attemptEv = new ChangeConsciousnessAttemptEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref attemptEv);
        if (attemptEv.Canceled)
            return;

        conscious.Comp1.Multiplier = newMult;
        var ev = new ConsciousnessChangedEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref ev);

        UpdateConsciousness(conscious, conscious.Comp1, conscious.Comp2);
        Dirty(conscious, conscious.Comp1);
    }

    public void AddConsciousnessCap(Entity<ConsciousnessComponent?, MobStateComponent?> conscious, FixedPoint2 capToAdd, bool warnIfOverflow = true)
    {
        if (!Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        if (capToAdd + conscious.Comp1.Cap > ConsciousnessComponent.MaxConsciousness)
        {
            Log.Warning($"Tried to add consciousness cap to {ToPrettyString(conscious.Owner)} " +
                        $"but it would exceed the maxConsciousness value. The result will be clamped to {ConsciousnessComponent.MaxConsciousness}");
        }
        var newCap = FixedPoint2.Clamp(capToAdd + conscious.Comp1.Modifier, 0, ConsciousnessComponent.MaxConsciousness);
        var delta = GetConsciousness(conscious) - CalculateNewConsciousness(conscious.Comp1, newConsciousnessCap:newCap);
        var attemptEv = new ChangeConsciousnessAttemptEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref attemptEv);
        if (attemptEv.Canceled)
            return;

        conscious.Comp1.Cap = newCap;
        var ev = new ConsciousnessChangedEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref ev);

        UpdateConsciousness(conscious, conscious.Comp1, conscious.Comp2);
        Dirty(conscious, conscious.Comp1);
    }

    public void SetMobStateOverride(Entity<ConsciousnessComponent?, MobStateComponent?> conscious, bool mobStateOverrideEnabled)
    {
        if (!Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        SetMobStateOverride(conscious, conscious.Comp1, conscious.Comp2, mobStateOverrideEnabled);
    }

    private void SetMobStateOverride(EntityUid consciousEntity, ConsciousnessComponent consciousness, MobStateComponent mobstate ,bool overrideEnabled)
    {
        consciousness.OverridenByMobstate = overrideEnabled;
        UpdateConsciousness(consciousEntity, consciousness, mobstate);
        Dirty(consciousEntity,consciousness);
    }

    public FixedPoint2 GetConsciousness(Entity<ConsciousnessComponent?> conscious)
    {
        return !Resolve(conscious, ref conscious.Comp) ? 0 : CalculateNewConsciousness(conscious.Comp);
    }

    public FixedPoint2 GetConsciousness(ConsciousnessComponent consciousness)
    {
        return CalculateNewConsciousness(consciousness);
    }

    public FixedPoint2 CalculateNewConsciousness(
        ConsciousnessComponent consciousness,
        FixedPoint2? newRawConsciousness = null,
        FixedPoint2? newConsciousnessMultiplier = null,
        FixedPoint2? newConsciousnessModifier = null,
        FixedPoint2? newConsciousnessCap = null)
    {
        newRawConsciousness ??= consciousness.RawValue;
        newConsciousnessMultiplier ??= consciousness.Multiplier;
        newConsciousnessModifier ??= consciousness.Modifier;
        newConsciousnessCap ??= consciousness.Cap;
        return CalculateConsciousness(
            newRawConsciousness.Value,
            newConsciousnessMultiplier.Value,
            newConsciousnessModifier.Value,
            newConsciousnessCap.Value);
    }


    public FixedPoint2 CalculateConsciousness(
        FixedPoint2 rawConsciousness,
        FixedPoint2 consciousnessMultiplier,
        FixedPoint2 consciousnessModifier,
        FixedPoint2 consciousnessCap)
    {
        return FixedPoint2.Clamp(
            rawConsciousness * consciousnessMultiplier + consciousnessModifier,
            0,
            consciousnessCap);
    }

    private void RaiseConsciousnessEvent<T>(EntityUid target, ref T eventToRaise) where T : struct
    {
        RaiseLocalEvent(target, ref eventToRaise);
    }

    public void UpdateConsciousness(Entity<ConsciousnessComponent?, MobStateComponent?> conscious)
    {
        if (!Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        UpdateConsciousness(conscious, conscious.Comp1, conscious.Comp2);
    }

    private void UpdateConsciousness(EntityUid consciousEnt, ConsciousnessComponent consciousness, MobStateComponent mobState)
    {
        var newConsciousness = CalculateNewConsciousness(consciousness);
        SetConscious(consciousEnt, consciousness, newConsciousness <= consciousness.Threshold);
        if (consciousness.OverridenByMobstate)
            return; //prevent any mobstate updates when override is enabled

        var attemptEv = new EntityConsciousnessKillAttemptEvent(new Entity<ConsciousnessComponent>(consciousEnt, consciousness));
        RaiseConsciousnessEvent(consciousEnt, ref attemptEv);
        if (!attemptEv.Canceled && newConsciousness <= 0 && mobState.CurrentState != MobState.Dead)
        {
            _mobStateSystem.UpdateMobState(consciousEnt, mobState);
        }
    }

    private void SetConscious(EntityUid consciousEnt,ConsciousnessComponent consciousness, bool isConscious)
    {
        if (consciousness.IsConscious == isConscious)
            return;
        var consciousPair = new Entity<ConsciousnessComponent>(consciousEnt, consciousness);
        if (isConscious)
        {
            var attemptEv = new EntityWakeUpAttemptEvent(consciousPair);
            RaiseConsciousnessEvent(consciousEnt,ref attemptEv);
            if (attemptEv.Canceled)
                return;
            consciousness.IsConscious = true;
            var ev = new EntityWakeUpEvent(consciousPair);
            RaiseConsciousnessEvent(consciousEnt,ref ev);
            Dirty(consciousPair);
        }
        else
        {
            var attemptEv = new EntityPassOutAttemptEvent(consciousPair);
            RaiseConsciousnessEvent(consciousEnt,ref attemptEv);
            if (attemptEv.Canceled)
                return;
            consciousness.IsConscious = false;
            var ev = new EntityPassOutEvent(consciousPair);
            RaiseConsciousnessEvent(consciousEnt,ref ev);
            Dirty(consciousPair);
        }
    }
}
