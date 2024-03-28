using System.Diagnostics.CodeAnalysis;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.HealthConditions.Components;
using Content.Shared.Medical.HealthConditions.Event;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.HealthConditions.Systems;

public sealed class HealthConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<HealthConditionManagerComponent, ComponentInit>(OnConditionManagerInit);
        SubscribeLocalEvent<HealthConditionManagerComponent, EntInsertedIntoContainerMessage>(OnConditionAddedToMan);
        SubscribeLocalEvent<HealthConditionManagerComponent, EntRemovedFromContainerMessage>(OnConditionRemovedFromMan);
    }

    /// <summary>
    /// Try to add a condition to a condition manager or try to get one if it already exists
    /// </summary>
    /// <param name="conditionManager">Target conditionManager entity/component</param>
    /// <param name="conditionId">Entity Prototype ID for the desired condition</param>
    /// <param name="newCondition">Condition entity/component pair that was created/found</param>
    /// <param name="severityOverride">Severity value to set after creating the condition</param>
    /// <param name="force">Should we ignore event cancellations</param>
    /// <param name="warnIfPresent">Log a Warning if this condition is already present</param>
    /// <returns>True if condition was added or already exists, false if condition could not be added</returns>
    public bool TryAddCondition(
        Entity<HealthConditionManagerComponent?> conditionManager,
        EntProtoId conditionId,
        [NotNullWhen(true)] out Entity<HealthConditionComponent>? newCondition,
        FixedPoint2? severityOverride = null,
        bool force = false,
        bool warnIfPresent = false)
    {
        newCondition = null;
        if (!Resolve(conditionManager, ref conditionManager.Comp))
            return false;

        if (conditionManager.Comp.ContainedConditionEntities.TryGetValue(conditionId, out var existingCondition))
        {
            if (warnIfPresent)
                Log.Warning($"Condition of type {conditionId} already exists on {ToPrettyString(conditionManager)}");
            newCondition = new Entity<HealthConditionComponent>(existingCondition, Comp<HealthConditionComponent>(existingCondition));
            if (severityOverride != null)
                SetConditionSeverity_Internal(newCondition.Value, severityOverride.Value);
            return true;
        }

        if (!TrySpawnInContainer(conditionId, conditionManager, HealthConditionManagerComponent.ContainerId, out var conditionEnt)
            || !TryComp<HealthConditionComponent>(conditionEnt, out var conditionComp))
        {
            return false;
        }

        newCondition = new Entity<HealthConditionComponent>(conditionEnt.Value, conditionComp);
        var attemptEv = new HealthConditionAddAttemptEvent(newCondition.Value);
        RaiseLocalEvent(conditionManager, ref attemptEv);

        if (!force && attemptEv.Canceled)
        {
            Del(conditionEnt);
            return false;
        }

        if (severityOverride != null)
            SetConditionSeverity_Internal(newCondition.Value, severityOverride.Value);
        return true;
    }


    /// <summary>
    /// Try to remove a condition from an entity
    /// </summary>
    /// <param name="conditionManager">Target conditionManager entity/component</param>
    /// <param name="condition">Condition entity/component pair to remove</param>
    /// <param name="force">Should we ignore event cancellations</param>
    /// <returns>True if successfully remove, false if not</returns>
    public bool TryRemoveCondition(
        Entity<HealthConditionManagerComponent?> conditionManager,
        Entity<HealthConditionComponent?> condition,
        bool force =false)
    {
        var conditionMeta = MetaData(condition);
        if (!Resolve(conditionManager, ref conditionManager.Comp)
            || !Resolve(condition, ref condition.Comp))
            return false;

        var validCondition = new Entity<HealthConditionComponent>(condition, condition.Comp);
        var attemptEv = new HealthConditionRemoveAttemptEvent(validCondition);
        RaiseLocalEvent(conditionManager, ref attemptEv);

        return force
               && !attemptEv.Canceled
               && _containerSystem.RemoveEntity(conditionManager, condition)
               && conditionMeta.EntityPrototype != null;
    }


    /// <summary>
    /// Try to remove a condition from an entity
    /// </summary>
    /// <param name="conditionManager">Target conditionManager entity/component</param>
    /// <param name="conditionId">Entity Prototype ID for the desired condition</param>
    /// <param name="force">Should we ignore event cancellations</param>
    /// <returns>True if successfully remove, false if not</returns>
    public bool TryRemoveCondition(
        Entity<HealthConditionManagerComponent?> conditionManager,
        EntProtoId conditionId,
        bool force = false)
    {
        if (!Resolve(conditionManager, ref conditionManager.Comp)
                || conditionManager.Comp.ContainedConditionEntities.TryGetValue(conditionId, out var conditionEnt))
        return false;
        return TryRemoveCondition(conditionManager, new Entity<HealthConditionComponent?>(conditionEnt, null),force);
    }


    /// <summary>
    /// Tries to get the condition entity associated with a conditionId if it is present
    /// </summary>
    /// <param name="conditionManager">Target conditionManager entity/component</param>
    /// <param name="conditionId">Entity Prototype ID for the desired condition</param>
    /// <param name="condition">The found Condition Entity/Comp pair</param>
    /// <returns>True if condition entity is found, false if not</returns>
    public bool TryGetCondition(Entity<HealthConditionManagerComponent?> conditionManager, EntProtoId conditionId,
        [NotNullWhen(true)] out Entity<HealthConditionComponent>? condition)
    {
        condition = null;
        if (!Resolve(conditionManager, ref conditionManager.Comp)
            || conditionManager.Comp.ContainedConditionEntities.TryGetValue(conditionId, out var conditionEnt))
            return false;
        condition = new Entity<HealthConditionComponent>(conditionEnt, Comp<HealthConditionComponent>(conditionEnt));
        return true;
    }

    /// <summary>
    /// Attempt to add severity to a condition. (This may be negative to subtract from severity)
    /// </summary>
    /// <param name="condition">Target Condition Entity/Comp</param>
    /// <param name="severityToAdd">Severity to add to the condition</param>
    /// <param name="force">Should we ignore event cancellations</param>
    /// <returns>True if Severity was added, false if not</returns>
    public bool TryAddConditionSeverity(Entity<HealthConditionComponent?> condition, FixedPoint2 severityToAdd, bool force = false)
    {
        if (!Resolve(condition, ref condition.Comp)
            || severityToAdd == 0)
            return false;
        var validCondition = new Entity<HealthConditionComponent>(condition, condition.Comp);
        var attemptEv = new HealthConditionSeverityChangeAttemptEvent(
            new Entity<HealthConditionComponent>(condition, condition.Comp), severityToAdd);
        RaiseConditionEvent(validCondition, ref attemptEv);
        if (!force && attemptEv.Canceled)
            return false;
        SetConditionSeverity_Internal(validCondition, condition.Comp.RawSeverity+ severityToAdd);
        return true;
    }

    /// <summary>
    /// Tries to set a condition's severity, try to use TryAddConditionSeverity instead because it doesn't override the
    /// existing severity value and risk causing the severity value to desync in other systems
    /// </summary>
    /// <param name="condition">Target Condition Entity/Comp</param>
    /// <param name="newSeverity">The new severity value</param>
    /// <param name="force">Should we ignore event cancellations</param>
    /// <returns>True if Severity was set to a new value, false if not</returns>
    public bool TrySetConditionSeverity(Entity<HealthConditionComponent?> condition, FixedPoint2 newSeverity, bool force = false)
    {
        if (!Resolve(condition, ref condition.Comp)
            || newSeverity == condition.Comp.RawSeverity)
            return false;
        var validCondition = new Entity<HealthConditionComponent>(condition, condition.Comp);
        var attempt2Ev = new HealthConditionSeveritySetAttemptEvent(
            new Entity<HealthConditionComponent>(condition, condition.Comp), newSeverity);
        var attempt1Ev = new HealthConditionSeverityChangeAttemptEvent(
            new Entity<HealthConditionComponent>(condition, condition.Comp), newSeverity-condition.Comp.RawSeverity);
        RaiseConditionEvent(validCondition, ref attempt1Ev);
        RaiseConditionEvent(validCondition, ref attempt2Ev);
        if (!force && (attempt1Ev.Canceled || attempt2Ev.Canceled))
            return false;
        SetConditionSeverity_Internal(validCondition, condition.Comp.RawSeverity+ newSeverity);
        return true;
    }

    #region InternalUse/Helpers

    /// <summary>
    /// Internal use only. Sets a condition's severity and raises the appropriate events.
    /// </summary>
    /// <param name="condition">Target Condition Entity/Comp</param>
    /// <param name="newSeverity">Severity we are setting</param>
    private void SetConditionSeverity_Internal(Entity<HealthConditionComponent> condition, FixedPoint2 newSeverity)
    {
        if (newSeverity == condition.Comp.RawSeverity)
            return;
        var oldSeverity = condition.Comp.RawSeverity;
        condition.Comp.RawSeverity = newSeverity;
        var clampedDelta = FixedPoint2.Clamp(newSeverity, 0, HealthConditionComponent.SeverityMax)
                           - FixedPoint2.Clamp( oldSeverity, 0, HealthConditionComponent.SeverityMax) ;
        if (clampedDelta == 0)
            return; //if the clamped delta is 0 don't do anything!
        var ev = new HealthConditionSeverityChangedEvent(condition, clampedDelta);
        RaiseConditionEvent(condition, ref ev);
        var ev2 = new HealthConditionSeveritySetEvent(condition, oldSeverity);
        RaiseConditionEvent(condition, ref ev2);
        Dirty(condition);
    }

    /// <summary>
    /// Raises a health condition event on both the Condition entity and Managing entity
    /// </summary>
    /// <param name="condition">Target Condition Entity/Comp</param>
    /// <param name="conditionEvent">Event being raised</param>
    /// <typeparam name="T">Event Type</typeparam>
    private void RaiseConditionEvent<T>(Entity<HealthConditionComponent> condition, ref T conditionEvent)
        where T : struct
    {
        RaiseLocalEvent(condition, ref conditionEvent);
        RaiseLocalEvent(condition.Comp.ConditionManager, ref conditionEvent);
    }

    #endregion

    #region EventHandlers

     private void OnConditionRemovedFromMan(EntityUid uid, HealthConditionManagerComponent man, ref EntRemovedFromContainerMessage args)
    {
        var conditionMeta = MetaData(args.Entity);
        if (conditionMeta.EntityPrototype == null)
        {
            Log.Error($"Entity without a prototype removed from inside Condition container on {ToPrettyString(uid)} This should never happen!");
            Del(args.Entity);
            return;
        }

        if (!TryComp<HealthConditionComponent>(args.Entity, out var condition))
        {
            Log.Error($"Entity without an condition component removed from inside Condition container on {ToPrettyString(uid)} This should never happen!");
            Del(args.Entity);
            return;
        }

        if (!man.ContainedConditionEntities.Remove(conditionMeta.EntityPrototype.ID))
        {
            Log.Error($"Condition of type {conditionMeta.EntityPrototype.ID} not found in {ToPrettyString(uid)}");
            return;
        }
        var ev = new HealthConditionRemovedEvent(new Entity<HealthConditionComponent>(args.Entity, condition));
        RaiseLocalEvent(uid, ref ev);
        Del(args.Entity);
        Dirty(uid, man);
    }

    private void OnConditionAddedToMan(EntityUid uid, HealthConditionManagerComponent man, ref EntInsertedIntoContainerMessage args)
    {
        var conditionMeta = MetaData(args.Entity);
        if (conditionMeta.EntityPrototype == null)
        {
            Log.Warning($"Entity without a prototype inserted into Condition container on {ToPrettyString(uid)}!");
            return;
        }

        if (!TryComp<HealthConditionComponent>(args.Entity, out var condition))
        {
            Log.Error($"Entity without an condition component added to Condition container on {ToPrettyString(uid)} This should never happen!");
            Del(args.Entity);
            return;
        }

        if (!man.ContainedConditionEntities.TryAdd(conditionMeta.EntityPrototype.ID, args.Entity))
        {
            Log.Error($"Condition of type {conditionMeta.EntityPrototype.ID} already exists on {ToPrettyString(uid)}");
            Del(args.Entity);
            return;
        }

        condition.ConditionManager = uid;
        var ev = new HealthConditionAddedEvent(new Entity<HealthConditionComponent>(args.Entity, Comp<HealthConditionComponent>(args.Entity)));
        RaiseLocalEvent(uid, ref ev);
        Dirty(uid, man);
    }

    private void OnConditionManagerInit(EntityUid uid, HealthConditionManagerComponent component, ref ComponentInit args)
    {
        _containerSystem.EnsureContainer<Container>(uid, HealthConditionManagerComponent.ContainerId);
    }


    #endregion

}
