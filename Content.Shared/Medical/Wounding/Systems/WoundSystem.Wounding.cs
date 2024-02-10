using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Events;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem
{
    private const float SplatterDamageMult = 1.0f;


    public WoundSystem(SharedContainerSystem containerSystem, IPrototypeManager prototypeManager, GibbingSystem gibbingSystem, DamageableSystem damageableSystem)
    {
        _containerSystem = containerSystem;
        _prototypeManager = prototypeManager;
        _gibbingSystem = gibbingSystem;
        _damageableSystem = damageableSystem;
    }

    private void InitWounding()
    {
        SubscribeLocalEvent<WoundableComponent, DamageChangedEvent>(OnWoundableDamaged);
    }

    private void OnWoundableDamaged(EntityUid owner, WoundableComponent woundable, ref DamageChangedEvent args)
    {
        //Do not handle damage if it is being set instead of being changed.
        //We will handle that with another listener
        if (args.DamageDelta == null)
            return;
        foreach (var (damageTypeId, damage) in args.DamageDelta.DamageDict)
        {
            TryApplyWounds(owner, damageTypeId, damage, woundable);
        }
    }

    /// <summary>
    /// Create a new wound on a woundable from the specified wound prototype
    /// </summary>
    /// <param name="woundableEnt">Target Woundable entity</param>
    /// <param name="woundPrototype">Prototype Id of the wound being spawned</param>
    /// <param name="woundable">Woundable Component</param>
    /// <param name="damageType">Damage type we are applying</param>
    /// <param name="damage">The amount of damage applied</param>
    /// <param name="force">Prevent canceling creating this wound</param>
    /// <returns>A woundable entity if successful, null if not</returns>
    public Entity<WoundComponent>? CreateWound(EntityUid  woundableEnt,EntProtoId woundPrototype,
        ProtoId<DamageTypePrototype> damageType, FixedPoint2 damage, WoundableComponent? woundable = null, bool force = false)
    {
        return !Resolve(woundableEnt, ref woundable) ? null : CreateWound_Internal(woundableEnt, woundPrototype, woundable, damageType, damage, force);
    }


    /// <summary>
    ///  Attempt to remove a wound
    /// </summary>
    /// <param name="woundEnt">The wound entity to remove</param>
    /// <param name="fullyHeal">Does this count as "fully healing" the wound, or just removal</param>
    /// <param name="woundComp">The wound component to remove</param>
    /// <param name="force">Prevent canceling the wound removal</param>
    /// <returns>True if successful, false if not</returns>
    public bool TryRemoveWound(EntityUid woundEnt, bool fullyHeal, WoundComponent? woundComp = null, bool force = false)
    {
        if (!Resolve(woundEnt, ref woundComp))
            return false;
        var woundableParent = woundComp.ParentWoundable;
        var woundable = new Entity<WoundableComponent>(woundableParent, Comp<WoundableComponent>(woundableParent));
        var wound = new Entity<WoundComponent>(woundEnt, woundComp);

        var onRemoveWoundAttempt = new RemoveWoundAttemptEvent(woundable, wound);
        RaiseRelayedWoundEvent(woundable, wound, ref onRemoveWoundAttempt);
        if (!force && onRemoveWoundAttempt.CancelRemove)
            return false;

        _containerSystem.TryRemoveFromContainer(woundEnt, true);

        if (fullyHeal)
        {
            var onWoundHealed = new WoundFullyHealedEvent(woundable, wound);
            RaiseRelayedWoundEvent(woundable, wound, ref onWoundHealed);
            woundable.Comp.HealthCap += wound.Comp.HealthDebuff/100 * wound.Comp.AppliedDamage;
            woundable.Comp.IntegrityCap += wound.Comp.IntegrityDebuff/100 * wound.Comp.AppliedDamage;
            woundable.Comp.Integrity += wound.Comp.IntegrityDamage/100 * wound.Comp.AppliedDamage;
        }
        else
        {
            var onWoundRemoved = new WoundRemovedEvent(woundable, wound);
            RaiseRelayedWoundEvent(woundable, wound, ref onWoundRemoved);
        }
        EntityManager.DeleteEntity(wound);
        return true;
    }


    /// <summary>
    /// Try to Create a new wound on a woundable from the specified wound prototype
    /// </summary>
    /// <param name="woundableEnt">Target Woundable entity</param>
    /// <param name="woundPrototype">Prototype Id of the wound being spawned</param>
    /// <param name="createdWound">The created wound</param>
    /// <param name="woundable">Woundable Component</param>
    /// <param name="damageType">Damage type we are applying</param>
    /// <param name="damage">The amount of damage applied</param>
    /// <param name="force">Prevent canceling creating this wound</param>
    /// <returns>True when successful, false when not</returns>
    public bool TryCreateWound(EntityUid woundableEnt, EntProtoId woundPrototype, [NotNullWhen(true)]out Entity<WoundComponent>? createdWound,
        ProtoId<DamageTypePrototype> damageType, FixedPoint2 damage, WoundableComponent? woundable = null, bool force = false)
    {
        createdWound = null;
        if (!Resolve(woundableEnt, ref woundable))
            return false;
        createdWound = CreateWound_Internal(woundableEnt, woundPrototype, woundable, damageType, damage, force);
        return createdWound != null;
    }

    /// <summary>
    /// Tries to get the appropriate wound for the specified damage type and damage amount
    /// </summary>
    /// <param name="woundableEnt">Woundable Entity</param>
    /// <param name="damageType">Damage type to check</param>
    /// <param name="damage">Damage being applied</param>
    /// <param name="woundProtoId">Found WoundProtoId</param>
    /// <param name="woundable">Woundable comp</param>
    /// <param name="overflow">The amount of damage exceeding the max cap</param>
    /// <returns>True if a woundProto is found, false if not</returns>
    public bool TryGetWoundProtoFromDamage(EntityUid woundableEnt,ProtoId<DamageTypePrototype> damageType, FixedPoint2 damage,
        [NotNullWhen(true)] out EntProtoId? woundProtoId, out FixedPoint2 overflow,
        WoundableComponent? woundable = null)
    {
        overflow = 0;
        woundProtoId = null;
        if (!Resolve(woundableEnt, ref woundable)
            || !woundable.Config.TryGetValue(damageType, out var metadata)
            )
            return false;
        var adjDamage = damage * metadata.Scaling;
        if (adjDamage > metadata.PoolDamageMax)
        {
            overflow = adjDamage - metadata.PoolDamageMax;
            adjDamage = metadata.PoolDamageMax;
        }
        var percentageOfMax = adjDamage * metadata.Scaling * 100 / (metadata.PoolDamageMax*100);
        var woundPool = _prototypeManager.Index(metadata.WoundPool);
        foreach (var (percentage, lastWoundProtoId) in woundPool.Wounds)
        {
            if (percentage > percentageOfMax)
                break;
            woundProtoId = lastWoundProtoId;
        }
        return woundProtoId != null;
    }

    public bool TryApplyWounds(EntityUid targetWoundable, ProtoId<DamageTypePrototype> damageType, FixedPoint2 damage,
        WoundableComponent? woundable = null, bool force = false)
    {
        if (!Resolve(targetWoundable, ref woundable)
            || !TryGetWoundProtoFromDamage(targetWoundable, damageType, damage, out var woundProtoId, out var overflow, woundable)
            || !TryCreateWound(targetWoundable, woundProtoId.Value, out var createdWound, damageType, damage, woundable))
            return false;
        //TODO: Apply overflow to adjacent/attached parts
        return true;
    }

    /// <summary>
    /// Create a new wound on a woundable from the specified wound prototype
    /// </summary>
    /// <param name="woundableEnt">Target Woundable entity</param>
    /// <param name="woundPrototype">Prototype Id of the wound being spawned</param>
    /// <param name="woundableComp">Woundable Component</param>
    /// <param name="damageType">Damage type used to create this wound</param>
    /// <param name="appliedDamage">The amount of damage applied to create this wound</param>
    /// <returns>A woundable entity if successful, null if not</returns>
    private Entity<WoundComponent>? CreateWound_Internal(EntityUid woundableEnt, EntProtoId woundPrototype,
        WoundableComponent woundableComp, ProtoId<DamageTypePrototype> damageType, FixedPoint2 appliedDamage, bool force)
    {
        if (EntityManager.TrySpawnInContainer(woundPrototype, woundableEnt,
                WoundableComponent.WoundableContainerId, out var woundEntId)
            || !TryComp(woundEntId, out WoundComponent? woundComp)
           )
            return null;
        var woundable = new Entity<WoundableComponent>(woundableEnt, woundableComp);
        var wound = new Entity<WoundComponent>(woundEntId.Value, woundComp);

        wound.Comp.Body = woundable.Comp.Body;
        wound.Comp.ParentWoundable = woundable.Owner;
        wound.Comp.RootWoundable = woundable.Comp.RootWoundable;
        wound.Comp.AppliedDamage = appliedDamage;
        wound.Comp.AppliedDamageType = damageType;
        var newWoundEvent = new CreateWoundAttemptEvent(woundable, wound);
        RaiseRelayedWoundEvent(woundable, wound, ref newWoundEvent);
        if (!force && newWoundEvent.Canceled)
        {
            EntityManager.DeleteEntity(woundEntId);
            return null;
        }
        ApplyWoundEffects(wound, woundable, damageType);
        return new Entity<WoundComponent>(woundEntId.Value, wound);
    }

    private void ApplyWoundEffects(Entity<WoundComponent> wound, Entity<WoundableComponent> woundable, ProtoId<DamageTypePrototype> damageType)
    {
        woundable.Comp.HealthCap -= wound.Comp.HealthDebuff/100 * wound.Comp.AppliedDamage;
        woundable.Comp.IntegrityCap -= wound.Comp.IntegrityDebuff/100 * wound.Comp.AppliedDamage;
        woundable.Comp.Integrity -= wound.Comp.IntegrityDamage/100 * wound.Comp.AppliedDamage;
        woundable.Comp.LastAppliedDamageType = damageType;
        var woundApplied = new WoundAppliedEvent(woundable, wound);
        RaiseRelayedWoundEvent(woundable, wound, ref woundApplied);
        Dirty(wound);
        ShouldGibWoundable(woundable.Owner, out var overflow, woundable.Comp, damageType);
    }

    /// <summary>
    /// Clamps the values of a woundable within the proper range.
    /// </summary>
    /// <param name="target">Entity that owns this woundable</param>
    /// <param name="woundable">Woundable being clamped</param>
    /// <returns>Returns true if integrity is zero or negative, returns false if invalid or if integrity is greater than 0</returns>
    public bool ClampWoundableValues(EntityUid target, WoundableComponent? woundable= null)
    {
        var dirty = false;
        if (!Resolve(target, ref woundable))
            return false;
        if (woundable.HealthCap < woundable.Health)
        {
            woundable.Health = woundable.HealthCap;
            dirty = true;
        }

        if (woundable.HealthCap < 0)
        {
            woundable.HealthCap = 0;
            dirty = true;
        }

        if (woundable.Health < 0)
        {
            woundable.Integrity += woundable.Health;
            woundable.Health = 0;
            dirty = true;
        }

        if (woundable.IntegrityCap < woundable.Integrity)
        {
            woundable.Integrity = woundable.IntegrityCap;
            dirty = true;
        }

        if (!dirty)
            Dirty(target, woundable);
        return woundable.Integrity <= 0;
    }


    /// <summary>
    /// Check to make sure woundable values are within thresholds and trigger gibbing if too much damage has been taken.
    /// This is automatically called when adding/removing wounds or applying damage. Manually call this if you modify a woundable's damage.
    /// </summary>
    /// <param name="target">The woundable entity</param>
    /// <param name="overflow">how much damage is left over after gibbing</param>
    /// <param name="woundable">The woundable component</param>
    /// <param name="damageTypeOverride">Optional override for the type of damage to use for overflow</param>
    /// <returns>True if we gibbed the part, false if we did not</returns>
    public bool ShouldGibWoundable(EntityUid target, out FixedPoint2 overflow,
        WoundableComponent? woundable = null, ProtoId<DamageTypePrototype>? damageTypeOverride = null)
    {

        overflow = 0;
        if (!Resolve(target, ref woundable))
            return false;
        var damageType = woundable.LastAppliedDamageType;
        if (damageTypeOverride != null)
            damageType = damageTypeOverride.Value;
        if (!ClampWoundableValues(target, woundable))
            return false;
        overflow = -woundable.Integrity;
        GibWoundable(target, woundable, damageType, overflow);
        return true;
    }

    private void GibWoundable(EntityUid woundableEnt, WoundableComponent woundable, ProtoId<DamageTypePrototype> damageType, FixedPoint2 splatDamage)
    {
        if (!_containerSystem.TryGetContainer(woundableEnt, WoundableComponent.WoundableContainerId, out var container))
            return;
        var woundCount = container.ContainedEntities.Count;
        foreach (var woundEnt in container.ContainedEntities)
        {
            TryRemoveWound(woundEnt, false);
        }

        var outerEnt = woundable.Body ?? woundable.RootWoundable;

        _gibbingSystem.TryGibEntity(outerEnt, woundableEnt, GibType.Gib, GibContentsOption.Drop, out var droppedEnts);
        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict.Add(damageType, splatDamage/woundCount * SplatterDamageMult);
        foreach (var targetEnt in droppedEnts)
        {
            _damageableSystem.TryChangeDamage(targetEnt, damageSpec);
        }
    }

    private void RaiseRelayedWoundEvent<T>(Entity<WoundableComponent> woundable, Entity<WoundComponent> wound,ref T woundEvent) where T : struct
    {
        RaiseLocalEvent(woundable.Owner, ref woundEvent);
        RaiseLocalEvent(wound.Owner, ref woundEvent);
    }

}
