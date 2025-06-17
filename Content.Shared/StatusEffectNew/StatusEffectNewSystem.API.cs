using System.Diagnostics.CodeAnalysis;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.StatusEffectNew;

public abstract partial class SharedStatusEffectsSystem
{
    /// <summary>
    /// Attempts to add a status effect to the specified entity. Returns True if the effect is added or exists
    /// and has been successfully extended in time, returns False if the status effect cannot be applied to this entity,
    /// or for any other reason.
    /// </summary>
    /// <param name="uid">The target entity to which the effect should be added.</param>
    /// <param name="effectProto">ProtoId of the status effect entity. Make sure it has StatusEffectComponent on it.</param>
    /// <param name="duration">Duration of status effect. Leave null and the effect will be permanent until it is removed using <c>TryRemoveStatusEffect</c>.</param>
    /// <param name="resetCooldown">
    /// If True, the effect duration time will be reset and reapplied. If False, the effect duration time will be overlaid with the existing one.
    /// In the other case, the effect will either be added for the specified time or its time will be extended for the specified time.
    /// </param>
    public bool TryAddStatusEffect(EntityUid uid,
        EntProtoId effectProto,
        TimeSpan? duration = null,
        bool resetCooldown = false)
    {
        if (TryGetStatusEffect(uid, effectProto, out var existedEffect))
        {
            //We don't need to add the effect if it already exists
            if (duration is null)
                return true;

            if (resetCooldown)
                SetStatusEffectTime(existedEffect.Value, duration.Value);
            else
                EditStatusEffectTime(existedEffect.Value, duration.Value);
            return true;
        }

        if (!CanAddStatusEffect(uid, effectProto))
            return false;

        EnsureComp<StatusEffectContainerComponent>(uid, out var container);

        //And only if all checks passed we spawn the effect
        var effect = PredictedSpawnAttachedTo(effectProto, Transform(uid).Coordinates);
        _transform.SetParent(effect, uid);
        if (!_effectQuery.TryComp(effect, out var effectComp))
            return false;

        if (duration != null)
            effectComp.EndEffectTime = _timing.CurTime + duration;

        container.ActiveStatusEffects.Add(effect);
        effectComp.AppliedTo = uid;

        var ev = new StatusEffectApplied(uid, (effect, effectComp));
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(effect, ref ev);

        return true;
    }

    /// <summary>
    /// Attempting to remove a status effect from an entity.
    /// Returns True if the status effect existed on the entity and was successfully removed, and False in otherwise.
    /// </summary>
    public bool TryRemoveStatusEffect(EntityUid uid, EntProtoId effectProto)
    {
        if (_net.IsClient) //We cant remove the effect on the client (we need someone more robust at networking than me)
            return false;

        if (!_containerQuery.TryComp(uid, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects)
        {
            var meta = MetaData(effect);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
            {
                if (!_effectQuery.TryComp(effect, out var effectComp))
                    return false;

                var ev = new StatusEffectRemoved(uid, (effect, effectComp));
                RaiseLocalEvent(uid, ref ev);
                RaiseLocalEvent(effect, ref ev);

                QueueDel(effect);
                container.ActiveStatusEffects.Remove(effect);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether the specified entity is under a specific status effect.
    /// </summary>
    public bool HasStatusEffect(EntityUid uid, EntProtoId effectProto)
    {
        if (!_containerQuery.TryComp(uid, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects)
        {
            var meta = MetaData(effect);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Attempting to retrieve the EntityUid of a status effect from an entity.
    /// </summary>
    public bool TryGetStatusEffect(EntityUid uid, EntProtoId effectProto, [NotNullWhen(true)] out EntityUid? effect)
    {
        effect = null;
        if (!_containerQuery.TryComp(uid, out var container))
            return false;

        foreach (var e in container.ActiveStatusEffects)
        {
            var meta = MetaData(e);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
            {
                effect = e;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempting to retrieve the time of a status effect from an entity.
    /// </summary>
    /// <param name="uid">The target entity on which the effect is applied.</param>
    /// <param name="effectProto">The prototype ID of the status effect to retrieve.</param>
    /// <param name="time">The output tuple containing the effect entity and its remaining time.</param>
    /// <param name="container">Optional. The status effect container component of the entity.</param>
    public bool TryGetTime(EntityUid uid,
        EntProtoId effectProto,
        out (EntityUid EffectEnt, TimeSpan? RemainigTime) time,
        StatusEffectContainerComponent? container = null)
    {
        time = default;
        if (!Resolve(uid, ref container))
            return false;

        foreach (var effect in container.ActiveStatusEffects)
        {
            var meta = MetaData(effect);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
            {
                if (!_effectQuery.TryComp(effect, out var effectComp))
                    return false;

                time = (effect, effectComp.EndEffectTime);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to edit the remaining time for a status effect on an entity.
    /// </summary>
    /// <param name="uid">The target entity on which the effect is applied.</param>
    /// <param name="effectProto">The prototype ID of the status effect to modify.</param>
    /// <param name="time">The time adjustment to apply to the status effect. Positive values extend the duration,
    /// while negative values reduce it.</param>
    public bool TryEditTime(EntityUid uid, EntProtoId effectProto, TimeSpan time)
    {
        if (!_containerQuery.TryComp(uid, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects)
        {
            var meta = MetaData(effect);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
            {
                EditStatusEffectTime(effect, time);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to set the remaining time for a status effect on an entity.
    /// </summary>
    /// <param name="uid">The target entity on which the effect is applied.</param>
    /// <param name="effectProto">The prototype ID of the status effect to modify.</param>
    /// <param name="time">The new duration for the status effect.</param>
    public bool TrySetTime(EntityUid uid, EntProtoId effectProto, TimeSpan time)
    {
        if (!_containerQuery.TryComp(uid, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects)
        {
            var meta = MetaData(effect);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
            {
                SetStatusEffectTime(effect, time);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the specified component is present on any of the entity's status effects.
    /// </summary>
    public bool HasEffectComp<T>(EntityUid target) where T : IComponent
    {
        if (!_containerQuery.TryComp(target, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects)
        {
            if (HasComp<T>(effect))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns all status effects that have the specified component
    /// </summary>
    public HashSet<Entity<T>> TryEffectWithComp<T>(EntityUid target) where T : IComponent
    {
        HashSet<Entity<T>> effects = new();
        if (!_containerQuery.TryComp(target, out var container))
            return effects;

        foreach (var effect in container.ActiveStatusEffects)
        {
            if (TryComp<T>(effect, out var comp))
                effects.Add((effect, comp));
        }

        return effects;
    }
}
