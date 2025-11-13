using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using YamlDotNet.Core.Tokens;

namespace Content.Shared.StatusEffectNew;

public sealed partial class StatusEffectsSystem
{
    /// <summary>
    /// Increments duration of status effect by <see cref="duration"/>.
    /// Tries to add status effect if it is not yet present on entity.
    /// </summary>
    /// <param name="target">The target entity to which the effect should be added.</param>
    /// <param name="effectProto">ProtoId of the status effect entity. Make sure it has StatusEffectComponent on it.</param>
    /// <param name="duration">Duration of status effect. Leave null and the effect will be permanent until it is removed using <c>TryRemoveStatusEffect</c>.</param>
    /// <param name="delay">The delay of the effect. If a start time already exists, the closest time takes precedence. Leave null for the effect to be instant.</param>
    /// <param name="statusEffect">The EntityUid of the status effect we have just created or null if it doesn't exist.</param>
    /// <returns>True if effect exists and its duration is set properly, false in case effect cannot be applied.</returns>
    public bool TryAddStatusEffectDuration(
        EntityUid target,
        EntProtoId effectProto,
        [NotNullWhen(true)] out EntityUid? statusEffect,
        TimeSpan duration,
        TimeSpan? delay = null
    )
    {
        if (duration == TimeSpan.Zero)
        {
            statusEffect = null;
            return false;
        }

        // We check to make sure time is greater than zero here because sometimes you want to use TryAddStatusEffect to remove duration instead...
        if (!TryGetStatusEffect(target, effectProto, out statusEffect))
            return TryAddStatusEffect(target, effectProto, out statusEffect, duration, delay);

        AddStatusEffectTime(statusEffect.Value, duration);
        UpdateStatusEffectDelay(statusEffect.Value, delay);

        return true;
    }


    ///<inheritdoc cref="TryAddStatusEffectDuration(EntityUid,EntProtoId,out EntityUid?,TimeSpan,TimeSpan?)"/>
    public bool TryAddStatusEffectDuration(EntityUid target, EntProtoId effectProto, TimeSpan duration, TimeSpan? delay = null)
    {
        return TryAddStatusEffectDuration(target, effectProto, out _, duration, delay);
    }

    /// <summary>
    /// Sets duration of status effect by <see cref="duration"/>.
    /// Tries to add status effect if it is not yet present on entity.
    /// </summary>
    /// <param name="target">The target entity to which the effect should be added.</param>
    /// <param name="effectProto">ProtoId of the status effect entity. Make sure it has StatusEffectComponent on it.</param>
    /// <param name="duration">Duration of status effect. Leave null and the effect will be permanent until it is removed using <c>TryRemoveStatusEffect</c>.</param>
    /// <param name="delay">The delay of the effect. If a start time already exists, the closest time takes precedence. Leave null for the effect to be instant.</param>
    /// <param name="statusEffect">The EntityUid of the status effect we have just created or null if it doesn't exist.</param>
    /// <returns>True if effect exists and its duration is set properly, false in case effect cannot be applied.</returns>
    public bool TrySetStatusEffectDuration(
        EntityUid target,
        EntProtoId effectProto,
        [NotNullWhen(true)] out EntityUid? statusEffect,
        TimeSpan? duration = null,
        TimeSpan? delay = null
    )
    {
        if (duration <= TimeSpan.Zero)
        {
            statusEffect = null;
            return false;
        }

        if (!TryGetStatusEffect(target, effectProto, out statusEffect))
            return TryAddStatusEffect(target, effectProto, out statusEffect, duration, delay);

        if (!_effectQuery.TryComp(statusEffect, out var statusEffectComponent))
            return false;

        var endTime = delay == null || statusEffectComponent.Applied ? _timing.CurTime + duration : _timing.CurTime + delay + duration;
        SetStatusEffectEndTime(statusEffect.Value, endTime);
        UpdateStatusEffectDelay(statusEffect.Value, delay);

        return true;
    }

    /// <inheritdoc cref="TrySetStatusEffectDuration(EntityUid,EntProtoId,out EntityUid?,TimeSpan?,TimeSpan?)"/>
    public bool TrySetStatusEffectDuration(EntityUid target, EntProtoId effectProto, TimeSpan? duration = null, TimeSpan? delay = null)
    {
        return TrySetStatusEffectDuration(target, effectProto, out _, duration, delay);
    }

    /// <summary>
    /// Updates duration of effect to larger value between provided <see cref="duration"/> and current effect duration.
    /// Tries to add status effect if it is not yet present on entity.
    /// </summary>
    /// <param name="target">The target entity to which the effect should be added.</param>
    /// <param name="effectProto">ProtoId of the status effect entity. Make sure it has StatusEffectComponent on it.</param>
    /// <param name="duration">Duration of status effect. Leave null and the effect will be permanent until it is removed using <c>TryRemoveStatusEffect</c>.</param>
    /// <param name="delay">The delay of the effect. If a start time already exists, the closest time takes precedence. Leave null for the effect to be instant.</param>
    /// <param name="statusEffect">The EntityUid of the status effect we have just created or null if it doesn't exist.</param>
    /// <returns>True if effect exists and its duration is set properly, false in case effect cannot be applied.</returns>
    public bool TryUpdateStatusEffectDuration(
        EntityUid target,
        EntProtoId effectProto,
        [NotNullWhen(true)] out EntityUid? statusEffect,
        TimeSpan? duration = null,
        TimeSpan? delay = null
    )
    {
        if (duration <= TimeSpan.Zero)
        {
            statusEffect = null;
            return false;
        }

        if (!TryGetStatusEffect(target, effectProto, out statusEffect))
            return TryAddStatusEffect(target, effectProto, out statusEffect, duration, delay);

        if (!_effectQuery.TryComp(statusEffect, out var statusEffectComponent))
            return false;

        var endTime = delay == null || statusEffectComponent.Applied ? duration : delay + duration;
        UpdateStatusEffectTime(statusEffect.Value, endTime);
        UpdateStatusEffectDelay(statusEffect.Value, delay);

        return true;
    }

    /// <inheritdoc cref="TryUpdateStatusEffectDuration(EntityUid,EntProtoId,out EntityUid?,TimeSpan?,TimeSpan?)"/>
    public bool TryUpdateStatusEffectDuration(EntityUid target, EntProtoId effectProto, TimeSpan? duration = null, TimeSpan? delay = null)
    {
        return TryUpdateStatusEffectDuration(target, effectProto, out _, duration, delay);
    }

    /// <summary>
    /// Attempting to remove a status effect from an entity.
    /// Returns True if the status effect existed on the entity and was successfully removed, and False in otherwise.
    /// </summary>
    public bool TryRemoveStatusEffect(EntityUid target, EntProtoId effectProto)
    {
        if (!_containerQuery.TryComp(target, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            var meta = MetaData(effect);

            if (meta.EntityPrototype is null
                || meta.EntityPrototype != effectProto)
                continue;

            if (!_effectQuery.HasComp(effect))
                return false;

            PredictedQueueDel(effect);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether the specified entity is under a specific status effect.
    /// </summary>
    public bool HasStatusEffect(EntityUid target, EntProtoId effectProto)
    {
        if (!_containerQuery.TryComp(target, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects?.ContainedEntities ?? [])
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
    public bool TryGetStatusEffect(EntityUid target, EntProtoId effectProto, [NotNullWhen(true)] out EntityUid? effect)
    {
        effect = null;
        if (!_containerQuery.TryComp(target, out var container))
            return false;

        foreach (var e in container.ActiveStatusEffects?.ContainedEntities ?? [])
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
    public bool TryGetTime(
        EntityUid uid,
        EntProtoId effectProto,
        out (EntityUid EffectEnt, TimeSpan? EndEffectTime, TimeSpan? StartEffectTime) time,
        StatusEffectContainerComponent? container = null
    )
    {
        time = default;
        if (!Resolve(uid, ref container))
            return false;

        foreach (var effect in container.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            var meta = MetaData(effect);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
            {
                if (!_effectQuery.TryComp(effect, out var effectComp))
                    return false;

                time = (effect, effectComp.EndEffectTime, effectComp.StartEffectTime);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to get the maximum time left for a given Status Effect Component, returns false if no such
    /// component exists.
    /// </summary>
    /// <param name="uid">The target entity on which the effect is applied.</param>
    /// <param name="time">Returns the EntityUid of the status effect with the most time left, and the end effect time
    /// of that status effect.</param>
    /// <returns> True if a status effect entity with the given component exists</returns>
    public bool TryGetMaxTime<T>(EntityUid uid, out (EntityUid EffectEnt, TimeSpan? EndEffectTime) time) where T : IComponent
    {
        time = default;
        if (!TryEffectsWithComp<T>(uid, out var status))
            return false;

        time.EndEffectTime = TimeSpan.Zero;

        foreach (var effect in status)
        {
            if (effect.Comp2.EndEffectTime == null)
            {
                time = (effect.Owner, null);
                return true;
            }

            if (effect.Comp2.EndEffectTime > time.EndEffectTime)
                time = (effect.Owner, effect.Comp2.EndEffectTime);
        }
        return true;
    }

    /// <summary>
    /// Attempts to edit the remaining time for a status effect on an entity.
    /// </summary>
    /// <param name="uid">The target entity on which the effect is applied.</param>
    /// <param name="effectProto">The prototype ID of the status effect to modify.</param>
    /// <param name="time">
    /// The time adjustment to apply to the status effect. Positive values extend the duration,
    /// while negative values reduce it.
    /// </param>
    /// <returns> True if duration was edited successfully, false otherwise.</returns>
    public bool TryAddTime(EntityUid uid, EntProtoId effectProto, TimeSpan time)
    {
        if (!_containerQuery.TryComp(uid, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            var meta = MetaData(effect);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
            {
                AddStatusEffectTime(effect, time);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A method which specifically removes time from a status effect, or removes the status effect if time is null.
    /// </summary>
    /// <param name="uid">The target entity on which the effect is applied.</param>
    /// <param name="effectProto">The prototype ID of the status effect to modify.</param>
    /// <param name="time">
    /// The time adjustment to apply to the status effect. Positive values extend the duration,
    /// while negative values reduce it.
    /// </param>
    /// <returns> True if duration was edited successfully, false otherwise.</returns>
    public bool TryRemoveTime(EntityUid uid, EntProtoId effectProto, TimeSpan? time)
    {
        return time == null ? TryRemoveStatusEffect(uid, effectProto) : TryAddTime(uid, effectProto, - time.Value);
    }

    /// <summary>
    /// Attempts to set the remaining time for a status effect on an entity.
    /// </summary>
    /// <param name="uid">The target entity on which the effect is applied.</param>
    /// <param name="effectProto">The prototype ID of the status effect to modify.</param>
    /// <param name="time">The new duration for the status effect.</param>
    /// <returns> True if duration was set successfully, false otherwise.</returns>
    public bool TrySetTime(EntityUid uid, EntProtoId effectProto, TimeSpan time)
    {
        if (!_containerQuery.TryComp(uid, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            var meta = MetaData(effect);
            if (meta.EntityPrototype is not null && meta.EntityPrototype == effectProto)
            {
                SetStatusEffectEndTime(effect, time);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the specified component is present on any of the entity's status effects.
    /// </summary>
    public bool HasEffectComp<T>(EntityUid? target) where T : IComponent
    {
        if (!_containerQuery.TryComp(target, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            if (HasComp<T>(effect))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns all status effects that have the specified component.
    /// </summary>
    public bool TryEffectsWithComp<T>(EntityUid? target, [NotNullWhen(true)] out HashSet<Entity<T, StatusEffectComponent>>? effects) where T : IComponent
    {
        effects = null;
        if (!_containerQuery.TryComp(target, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            if (!_effectQuery.TryComp(effect, out var statusComp))
                continue;

            if (TryComp<T>(effect, out var comp))
            {
                effects ??= [];
                effects.Add((effect, comp, statusComp));
            }
        }

        return effects is not null;
    }

    /// <summary>
    /// Helper function for calculating how long it takes for all effects with a particular component to disappear. Useful for overlays.
    /// </summary>
    /// <param name="target">An entity from which status effects are checked.</param>
    /// <param name="endTime">The farthest end time of effects with this component is returned. Can be null if one of the effects is infinite.</param>
    /// <returns>True if effects with the specified component were found, or False if there are no such effects.</returns>
    public bool TryGetEffectsEndTimeWithComp<T>(EntityUid? target, out TimeSpan? endTime) where T : IComponent
    {
        endTime = _timing.CurTime;
        if (!_containerQuery.TryComp(target, out var container))
            return false;

        foreach (var effect in container.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            if (!HasComp<T>(effect))
                continue;

            if (!_effectQuery.TryComp(effect, out var statusComp))
                continue;

            if (statusComp.EndEffectTime is null)
            {
                endTime = null;
                return true; //This effect never ends, so we return null at endTime, but return true that there is time.
            }

            if (statusComp.EndEffectTime > endTime)
                endTime = statusComp.EndEffectTime;
        }

        return endTime is not null;
    }
}
