using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
using Content.Shared.Medical.Consciousness.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Medical.Consciousness.Systems;

public sealed class ConsciousnessSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsciousnessComponent,UpdateMobStateEvent>(OnMobstateChanged);
        SubscribeLocalEvent<ConsciousnessComponent,MapInitEvent>(ConsciousnessInit, after: [typeof(SharedBodySystem)]);
        //Prevent providers from being added twice (once on client/server). Eventually replace with proper overrides
        if (!_netManager.IsClient)
        { // TODO: this might be causing the spawn dead problem
            SubscribeLocalEvent<ConsciousnessProviderComponent, OrganRemovedFromBodyEvent>(OnProviderRemoved);
            SubscribeLocalEvent<ConsciousnessProviderComponent, OrganAddedToBodyEvent>(OnProviderAdded);
        }
    }

    private void OnProviderAdded(EntityUid providerUid, ConsciousnessProviderComponent provider, ref OrganAddedToBodyEvent args)
    {
        if (!TryComp<ConsciousnessComponent>(args.Body, out var consciousness))
            return;
        AddConsciousnessProvider(
            new Entity<ConsciousnessComponent>(args.Body, consciousness),
            new Entity<ConsciousnessProviderComponent>(providerUid, provider), args.Body.Comp.BodyInitialized);
    }

    private void OnProviderRemoved(EntityUid providerUid, ConsciousnessProviderComponent provider, ref OrganRemovedFromBodyEvent args)
    {
        if (!TryComp<ConsciousnessComponent>(args.OldBody, out var consciousness)
            || consciousness.LinkedProviders.Count != 0
            || !consciousness.LinkedProviders.Remove(providerUid))
            return;
        RemoveConsciousnessProvider(
            new Entity<ConsciousnessComponent>(args.OldBody, consciousness),
            new Entity<ConsciousnessProviderComponent>(providerUid, provider), args.OldBody.Comp.BodyInitialized);
    }

    private void ConsciousnessInit(EntityUid uid, ConsciousnessComponent consciousness, MapInitEvent args)
    {
        UpdateConsciousnessState(
            new Entity<ConsciousnessComponent?, MobStateComponent?>(uid, consciousness, null));
    }

    private void OnMobstateChanged(EntityUid uid, ConsciousnessComponent consciousness, ref UpdateMobStateEvent args)
    {
        //Do nothing if mobstate handling is set to be overriden or if we are conscious
        if (consciousness.OverridenByMobstate || consciousness.Consciousness > 0)
            return;
        args.State = MobState.Dead;
        var ev = new EntityConsciousnessKillEvent(new Entity<ConsciousnessComponent>(uid, consciousness));
        RaiseConsciousnessEvent(uid, ref ev);
    }

    public void ChangeConsciousness(Entity<ConsciousnessComponent?, MobStateComponent?> conscious, FixedPoint2 consciousnessDelta,
        bool updateState = true)
    {
        if (consciousnessDelta == 0 || !Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        var delta = FixedPoint2.Clamp(
            conscious.Comp1.RawValue+consciousnessDelta * conscious.Comp1.Multiplier +  conscious.Comp1.Modifier,
            0,
            conscious.Comp1.Cap) -  conscious.Comp1.Consciousness;
        var attemptEv = new ChangeConsciousnessAttemptEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref attemptEv);
        if (attemptEv.Canceled)
            return;

        conscious.Comp1.RawValue += consciousnessDelta;
        Dirty(conscious.Owner, conscious.Comp1);
        if (delta == 0)
            return;
        var ev = new ConsciousnessChangedEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref ev);
        if (updateState)
            UpdateConsciousnessState(conscious, conscious.Comp1, conscious.Comp2);
        Dirty(conscious, conscious.Comp1);
    }

    public void ChangeConsciousnessModifier(Entity<ConsciousnessComponent?, MobStateComponent?> conscious,
        FixedPoint2 modifierDelta, bool updateState = true)
    {
        if (modifierDelta == 0 || !Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        var delta = FixedPoint2.Clamp(
            conscious.Comp1.RawValue * conscious.Comp1.Multiplier +  conscious.Comp1.Modifier+modifierDelta,
            0,
            conscious.Comp1.Cap) -  conscious.Comp1.Consciousness;
        var attemptEv = new ChangeConsciousnessAttemptEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref attemptEv);
        if (attemptEv.Canceled)
            return;

        conscious.Comp1.Modifier += modifierDelta;
        Dirty(conscious.Owner, conscious.Comp1);
        if (delta == 0)
            return;
        var ev = new ConsciousnessChangedEvent(new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref ev);

        if (updateState)
            UpdateConsciousnessState(conscious, conscious.Comp1, conscious.Comp2);
        Dirty(conscious, conscious.Comp1);
    }

    public void ChangeConsciousnessMultiplier(Entity<ConsciousnessComponent?, MobStateComponent?> conscious,
        FixedPoint2 multiplierDelta, bool updateState = true)
    {
        if (multiplierDelta == 0 || !Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        var delta = FixedPoint2.Clamp(
            conscious.Comp1.RawValue * (conscious.Comp1.Multiplier+multiplierDelta) + conscious.Comp1.Modifier,
            0,
            conscious.Comp1.Cap) -  conscious.Comp1.Consciousness;
        var attemptEv = new ChangeConsciousnessAttemptEvent(
            new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref attemptEv);
        if (attemptEv.Canceled)
            return;

        conscious.Comp1.Multiplier += multiplierDelta;
        Dirty(conscious.Owner, conscious.Comp1);
        if (delta == 0)
            return;
        var ev = new ConsciousnessChangedEvent(
            new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref ev);

        if (updateState)
            UpdateConsciousnessState(conscious, conscious.Comp1, conscious.Comp2);
        Dirty(conscious, conscious.Comp1);
    }

    public void ChangeConsciousnessCap(Entity<ConsciousnessComponent?, MobStateComponent?> conscious, FixedPoint2 capDelta,
        bool updateState = true)
    {
        if (capDelta == 0 || !Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        var newCap = FixedPoint2.Clamp(
            capDelta + conscious.Comp1.Modifier, 0, ConsciousnessComponent.MaxConsciousness);

        var delta = FixedPoint2.Clamp(
            conscious.Comp1.RawValue * (conscious.Comp1.Multiplier) + conscious.Comp1.Modifier,
            0,
            conscious.Comp1.Cap+capDelta) -  conscious.Comp1.Consciousness;
        var attemptEv = new ChangeConsciousnessAttemptEvent(
            new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref attemptEv);
        if (attemptEv.Canceled)
            return;

        conscious.Comp1.RawCap = newCap;
        Dirty(conscious.Owner, conscious.Comp1);
        if (delta == 0)
            return;
        var ev = new ConsciousnessChangedEvent(
            new Entity<ConsciousnessComponent>(conscious, conscious.Comp1), delta);
        RaiseConsciousnessEvent(conscious, ref ev);

        if (updateState)
            UpdateConsciousnessState(conscious, conscious.Comp1, conscious.Comp2);
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
        UpdateConsciousnessState(consciousEntity, consciousness, mobstate);
        Dirty(consciousEntity,consciousness);
    }

    private void RaiseConsciousnessEvent<T>(EntityUid target, ref T eventToRaise) where T : struct
    {
        RaiseLocalEvent(target, ref eventToRaise);
    }

    public void UpdateConsciousnessState(Entity<ConsciousnessComponent?, MobStateComponent?> conscious)
    {
        if (!Resolve(conscious, ref conscious.Comp1, ref conscious.Comp2))
            return;
        UpdateConsciousnessState(conscious, conscious.Comp1, conscious.Comp2);
    }

    public void AddConsciousnessProvider(Entity<ConsciousnessComponent> consciousness, Entity<ConsciousnessProviderComponent> provider,
        bool initializing = false)
    {
        if (consciousness.Comp.LinkedProviders.Count < consciousness.Comp.ExpectedProviderCount)
        {
            consciousness.Comp.LinkedProviders.Add(provider);
            provider.Comp.LinkedConsciousness = consciousness.Owner;
            Dirty(consciousness, consciousness.Comp);
            if (initializing)
                return; //Do not change consciousness if we are still initializing
            ChangeConsciousness(
                new Entity<ConsciousnessComponent?, MobStateComponent?>(consciousness, consciousness, null),
                1/(float)consciousness.Comp.ExpectedProviderCount * ConsciousnessComponent.MaxConsciousness);
            return;
        }
        Log.Error($"Tried to add consciousness provider to {ToPrettyString(consciousness)} " +
                  $"which already has maximum number of providers!");
    }

    public void RemoveConsciousnessProvider(Entity<ConsciousnessComponent> consciousness, Entity<ConsciousnessProviderComponent> provider,
    bool initializing = false)
    {
        if (consciousness.Comp.LinkedProviders.Count > 0 && consciousness.Comp.LinkedProviders.Remove(provider.Owner))
        {
            provider.Comp.LinkedConsciousness = null;
            Dirty(consciousness);

            //Do not change consciousness if we are still initializing, we shouldn't normally be removing parts during
            //init but just in case we want to check this.
            if (!initializing)
                return;
            ChangeConsciousness(
                new Entity<ConsciousnessComponent?, MobStateComponent?>(consciousness, consciousness, null),
                -1/(float)consciousness.Comp.ExpectedProviderCount * ConsciousnessComponent.MaxConsciousness);
            return;
        }
        Log.Error($"Tried to remove consciousness provider from {ToPrettyString(consciousness)} " +
                  $"which was not found in it's provider list, or the list was empty!");
    }
    private void UpdateConsciousnessState(EntityUid consciousEnt, ConsciousnessComponent consciousness, MobStateComponent mobState)
    {
        var consciousnessValue = consciousness.Consciousness;
        SetConscious(consciousEnt, consciousness, consciousnessValue  <= consciousness.RawThreshold);
        if (consciousness.OverridenByMobstate)
            return; //prevent any mobstate updates when override is enabled

        var attemptEv = new EntityConsciousnessKillAttemptEvent(new Entity<ConsciousnessComponent>(consciousEnt, consciousness));
        RaiseConsciousnessEvent(consciousEnt, ref attemptEv);
        if (!attemptEv.Canceled && consciousnessValue <= 0 && mobState.CurrentState != MobState.Dead)
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
