using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.StatusEffectNew;

public sealed partial class SharedStatusEffectNewSystem
{
    /// <summary>
    /// Attempts to add a status effect to the specified entity. Returns True if the effect is added or exists
    /// and has been successfully extended in time, returns False if the status effect cannot be applied to this entity,
    /// or for any other reason
    /// </summary>
    /// <param name="uid">The target entity on which the effect is added</param>
    /// <param name="effectProto">ProtoId of the status effect entity. Make sure it has CP14StatusEffectComponent on it</param>
    /// <param name="duration">Duration of status effect. Leave null and the effect will be permanent until it is removed using <c>TryRemoveStatusEffect</c></param>
    /// <param name="resetCooldown">if True, the effect duration time will be reset and reapplied. If False, the effect duration time will be overlaid with the existing one.
    /// In the other case, the effect will either be added for the specified time or its time will be extended for the specified time.</param>
    /// <returns></returns>
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

            if (existedEffect != null)
            {
                if (resetCooldown)
                    SetStatusEffectTime(existedEffect.Value, duration.Value);
                else
                    AddStatusEffectTime(existedEffect.Value, duration.Value);
                return true;
            }
        }

        // We make this checks before status effect entity spawned
        if (!_proto.TryIndex(effectProto, out var effectProtoData))
            return false;

        if (!effectProtoData.TryGetComponent<StatusEffectNewComponent>(out var effectProtoComp,
                _compFactory))
        {
            Log.Error(
                $"Entity {effectProto} does not have a {nameof(StatusEffectNewComponent)} component, but tried to apply it as a status effect on {ToPrettyString(uid)}.");
            return false;
        }

        if (!_whitelist.CheckBoth(uid, effectProtoComp.Blacklist, effectProtoComp.Whitelist))
            return false;

        //Technically, on the client, all checks have been successful and we do not need to execute further code related to entity spawning, as this is the server's responsibility
        if (_net.IsClient)
            return true;

        EnsureComp<StatusEffectContainerComponent>(uid, out var container);

        //And only if all checks passed we spawn the effect
        var effect = SpawnAttachedTo(effectProto, Transform(uid).Coordinates);
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
    /// Attempting to remove a status effect from an entity. Returns True if the status effect existed on the entity and was successfully removed, and False in any other case.
    /// </summary>
    public bool TryRemoveStatusEffect(EntityUid uid, EntProtoId effectProto)
    {
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
    public bool TryGetStatusEffect(EntityUid uid, EntProtoId effectProto, out EntityUid? effect)
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
}
