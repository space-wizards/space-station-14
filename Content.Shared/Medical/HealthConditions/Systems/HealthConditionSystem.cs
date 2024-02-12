using System.Diagnostics.CodeAnalysis;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.HealthConditions.Components;
using Content.Shared.Medical.HealthConditions.Event;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.HealthConditions.Systems;

public sealed class AfflictionSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<HealthConditionManagerComponent, ComponentInit>(OnConditionManagerInit);
        SubscribeLocalEvent<HealthConditionManagerComponent, EntInsertedIntoContainerMessage>(OnConditionAddedToMan);
        SubscribeLocalEvent<HealthConditionManagerComponent, EntRemovedFromContainerMessage>(OnConditionRemovedFromMan);
    }

    private void OnConditionRemovedFromMan(EntityUid uid, HealthConditionManagerComponent man, ref EntRemovedFromContainerMessage args)
    {
        var afflictionMeta = MetaData(args.Entity);
        if (afflictionMeta.EntityPrototype == null)
        {
            Log.Error($"Entity without a prototype removed from inside Affliction container on {ToPrettyString(uid)} This should never happen!");
            Del(args.Entity);
            return;
        }

        if (!TryComp<HealthConditionComponent>(args.Entity, out var affliction))
        {
            Log.Error($"Entity without an affliction component removed from inside Affliction container on {ToPrettyString(uid)} This should never happen!");
            Del(args.Entity);
            return;
        }

        if (!man.ContainedConditionEntities.Remove(afflictionMeta.EntityPrototype.ID))
        {
            Log.Error($"Affliction of type {afflictionMeta.EntityPrototype.ID} not found in {ToPrettyString(uid)}");
            return;
        }
        var ev = new HealthConditionRemovedEvent(new Entity<HealthConditionComponent>(args.Entity, affliction));
        RaiseLocalEvent(uid, ref ev);
        Del(args.Entity);
        Dirty(uid, man);
    }

    private void OnConditionAddedToMan(EntityUid uid, HealthConditionManagerComponent man, ref EntInsertedIntoContainerMessage args)
    {
        var afflictionMeta = MetaData(args.Entity);
        if (afflictionMeta.EntityPrototype == null)
        {
            Log.Warning($"Entity without a prototype inserted into Affliction container on {ToPrettyString(uid)}!");
            return;
        }

        if (!TryComp<HealthConditionComponent>(args.Entity, out var affliction))
        {
            Log.Error($"Entity without an affliction component added to Affliction container on {ToPrettyString(uid)} This should never happen!");
            Del(args.Entity);
            return;
        }

        if (!man.ContainedConditionEntities.TryAdd(afflictionMeta.EntityPrototype.ID, args.Entity))
        {
            Log.Error($"Affliction of type {afflictionMeta.EntityPrototype.ID} already exists on {ToPrettyString(uid)}");
            Del(args.Entity);
            return;
        }

        affliction.ConditionManager = uid;
        var ev = new HealthConditionAddedEvent(new Entity<HealthConditionComponent>(args.Entity, Comp<HealthConditionComponent>(args.Entity)));
        RaiseLocalEvent(uid, ref ev);
        Dirty(uid, man);
    }

    private void OnConditionManagerInit(EntityUid uid, HealthConditionManagerComponent component, ref ComponentInit args)
    {
        _containerSystem.EnsureContainer<Container>(uid, HealthConditionManagerComponent.ContainerId);
    }

    public bool AddCondition(
        Entity<HealthConditionManagerComponent?> afflictionMan,
        EntProtoId afflictionProto,
        [NotNullWhen(true)] out Entity<HealthConditionComponent>? newAffliction,
        bool warnIfPresent = false)
    {
        newAffliction = null;
        if (!Resolve(afflictionMan, ref afflictionMan.Comp))
            return false;

        if (afflictionMan.Comp.ContainedConditionEntities.TryGetValue(afflictionProto, out var existingAffliction))
        {
            if (warnIfPresent)
                Log.Warning($"Affliction of type {afflictionProto} already exists on {ToPrettyString(afflictionMan)}");
            newAffliction = new Entity<HealthConditionComponent>(existingAffliction, Comp<HealthConditionComponent>(existingAffliction));
            return true;
        }

        if (!TrySpawnInContainer(afflictionProto, afflictionMan, HealthConditionManagerComponent.ContainerId, out var afflictionEnt)
            || !TryComp<HealthConditionComponent>(afflictionEnt, out var afflictionComp))
        {
            return false;
        }

        newAffliction = new Entity<HealthConditionComponent>(afflictionEnt.Value, afflictionComp);
        var attemptEv = new HealthConditionAddAttemptEvent(newAffliction.Value);

        RaiseLocalEvent(afflictionMan, ref attemptEv);
        if (!attemptEv.Canceled)
            return true;
        Del(afflictionEnt);
        return false;
    }

    public bool RemoveAffliction(Entity<HealthConditionManagerComponent?> afflictionMan, Entity<HealthConditionComponent?> affliction)
    {
        var afflictionMeta = MetaData(affliction);
        if (!Resolve(afflictionMan, ref afflictionMan.Comp)
            || !Resolve(affliction, ref affliction.Comp))
            return false;

        var validAffliction = new Entity<HealthConditionComponent>(affliction, affliction.Comp);
        var attemptEv = new HealthConditionRemoveAttemptEvent(validAffliction);
        RaiseLocalEvent(afflictionMan, ref attemptEv);

        return !attemptEv.Canceled
               && _containerSystem.RemoveEntity(afflictionMan, affliction)
               && afflictionMeta.EntityPrototype != null;
    }

    public bool RemoveCondition(Entity<HealthConditionManagerComponent?> afflictionMan, EntProtoId afflictionId)
    {
        if (!Resolve(afflictionMan, ref afflictionMan.Comp)
                || afflictionMan.Comp.ContainedConditionEntities.TryGetValue(afflictionId, out var afflictionEnt))
        return false;
        return RemoveAffliction(afflictionMan, new Entity<HealthConditionComponent?>(afflictionEnt, null));
    }

    public bool TryGetCondition(Entity<HealthConditionManagerComponent?> afflictionMan, EntProtoId afflictionId,
        [NotNullWhen(true)] out Entity<HealthConditionComponent>? affliction)
    {
        affliction = null;
        if (!Resolve(afflictionMan, ref afflictionMan.Comp)
            || afflictionMan.Comp.ContainedConditionEntities.TryGetValue(afflictionId, out var afflictionEnt))
            return false;
        affliction = new Entity<HealthConditionComponent>(afflictionEnt, Comp<HealthConditionComponent>(afflictionEnt));
        return true;
    }

    public void AddConditionSeverity(Entity<HealthConditionComponent?> condition, FixedPoint2 severityToAdd)
    {
        if (!Resolve(condition, ref condition.Comp)
            || severityToAdd == 0)
            return;
        var attemptEv = new HealthConditionSeverityChangeAttemptEvent(new Entity<HealthConditionComponent>(condition, condition.Comp), severityToAdd);
        RaiseLocalEvent(condition, ref attemptEv);
        RaiseLocalEvent(condition.Comp.ConditionManager, ref attemptEv);
        if (attemptEv.Canceled)
            return;
        condition.Comp.Severity = FixedPoint2.Clamp(condition.Comp.Severity + severityToAdd, 0, HealthConditionComponent.SeverityMax);

        var ev = new HealthConditionSeverityChangedEvent(new Entity<HealthConditionComponent>(condition, condition.Comp), severityToAdd);
        RaiseLocalEvent(condition, ref ev);
        RaiseLocalEvent(condition.Comp.ConditionManager, ref ev);
        Dirty(condition);
    }

}
