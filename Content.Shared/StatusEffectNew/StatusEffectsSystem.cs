using System.Diagnostics.CodeAnalysis;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// This system controls status effects, their lifetime, and provides an API for adding them to entities,
/// removing them from entities, or getting information about current effects on entities.
/// </summary>
public sealed partial class StatusEffectsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<StatusEffectContainerComponent> _containerQuery;
    private EntityQuery<StatusEffectComponent> _effectQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRelay();

        SubscribeLocalEvent<StatusEffectContainerComponent, ComponentInit>(OnStatusContainerInit);
        SubscribeLocalEvent<StatusEffectContainerComponent, ComponentShutdown>(OnStatusContainerShutdown);
        SubscribeLocalEvent<StatusEffectContainerComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<StatusEffectContainerComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);

        SubscribeLocalEvent<RejuvenateRemovedStatusEffectComponent, StatusEffectRelayedEvent<RejuvenateEvent>>(OnRejuvenate);

        _containerQuery = GetEntityQuery<StatusEffectContainerComponent>();
        _effectQuery = GetEntityQuery<StatusEffectComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StatusEffectComponent>();
        while (query.MoveNext(out var ent, out var effect))
        {
            if (effect.EndEffectTime is null)
                continue;

            if (!(_timing.CurTime >= effect.EndEffectTime))
                continue;

            if (effect.AppliedTo is null)
                continue;

            var meta = MetaData(ent);
            if (meta.EntityPrototype is null)
                continue;

            TryRemoveStatusEffect(effect.AppliedTo.Value, meta.EntityPrototype);
        }
    }

    private void OnStatusContainerInit(Entity<StatusEffectContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.ActiveStatusEffects =
            _container.EnsureContainer<Container>(ent, StatusEffectContainerComponent.ContainerId);
        // We show the contents of the container to allow status effects to have visible sprites.
        ent.Comp.ActiveStatusEffects.ShowContents = true;
        ent.Comp.ActiveStatusEffects.OccludesLight = false;
    }

    private void OnStatusContainerShutdown(Entity<StatusEffectContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActiveStatusEffects is { } container)
            _container.ShutdownContainer(container);
    }

    private void OnEntityInserted(Entity<StatusEffectContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != StatusEffectContainerComponent.ContainerId)
            return;

        if (!TryComp<StatusEffectComponent>(args.Entity, out var statusComp))
            return;

        // Make sure AppliedTo is set correctly so events can rely on it
        if (statusComp.AppliedTo != ent)
        {
            statusComp.AppliedTo = ent;
            Dirty(args.Entity, statusComp);
        }

        var ev = new StatusEffectAppliedEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);
    }

    private void OnEntityRemoved(Entity<StatusEffectContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != StatusEffectContainerComponent.ContainerId)
            return;

        if (!TryComp<StatusEffectComponent>(args.Entity, out var statusComp))
            return;

        var ev = new StatusEffectRemovedEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);

        // Clear AppliedTo after events are handled so event handlers can use it.
        if (statusComp.AppliedTo == null)
            return;

        // Why not just delete it? Well, that might end up being best, but this
        // could theoretically allow for moving status effects from one entity
        // to another. That might be good to have for polymorphs or something.
        statusComp.AppliedTo = null;
        Dirty(args.Entity, statusComp);
    }

    private void OnRejuvenate(Entity<RejuvenateRemovedStatusEffectComponent> ent,
        ref StatusEffectRelayedEvent<RejuvenateEvent> args)
    {
        PredictedQueueDel(ent.Owner);
    }

    public bool CanAddStatusEffect(EntityUid uid, EntProtoId effectProto)
    {
        if (!_proto.TryIndex(effectProto, out var effectProtoData))
            return false;

        if (!effectProtoData.TryGetComponent<StatusEffectComponent>(out var effectProtoComp, Factory))
            return false;

        if (!_whitelist.CheckBoth(uid, effectProtoComp.Blacklist, effectProtoComp.Whitelist))
            return false;

        var ev = new BeforeStatusEffectAddedEvent(effectProto);
        RaiseLocalEvent(uid, ref ev);

        if (ev.Cancelled)
            return false;

        return true;
    }

    /// <summary>
    /// Attempts to add a status effect to the specified entity. Returns True if the effect is added, does not check if one
    /// already exists as it's intended to be called after a check for an existing effect has already failed.
    /// </summary>
    /// <param name="target">The target entity to which the effect should be added.</param>
    /// <param name="effectProto">ProtoId of the status effect entity. Make sure it has StatusEffectComponent on it.</param>
    /// <param name="duration">Duration of status effect. Leave null and the effect will be permanent until it is removed using <c>TryRemoveStatusEffect</c>.</param>
    /// <param name="statusEffect">The EntityUid of the status effect we have just created or null if we couldn't create one.</param>
    private bool TryAddStatusEffect(
        EntityUid target,
        EntProtoId effectProto,
        [NotNullWhen(true)] out EntityUid? statusEffect,
        TimeSpan? duration = null
    )
    {
        statusEffect = null;

        if (duration <= TimeSpan.Zero)
            return false;

        if (!CanAddStatusEffect(target, effectProto))
            return false;

        EnsureComp<StatusEffectContainerComponent>(target);

        // And only if all checks passed we spawn the effect
        if (!PredictedTrySpawnInContainer(effectProto,
                target,
                StatusEffectContainerComponent.ContainerId,
                out var effect))
            return false;

        if (!_effectQuery.TryComp(effect, out var effectComp))
            return false;

        statusEffect = effect;
        SetStatusEffectEndTime((effect.Value, effectComp), _timing.CurTime + duration);

        return true;
    }

    private void UpdateStatusEffectTime(Entity<StatusEffectComponent?> effect, TimeSpan? duration)
    {
        if (!_effectQuery.Resolve(effect, ref effect.Comp))
            return;

        // It's already infinitely long
        if (effect.Comp.EndEffectTime is null)
            return;

        TimeSpan? newEndTime = null;

        if (duration is not null)
        {
            // Don't update time to a smaller timespan...
            newEndTime = _timing.CurTime + duration;
            if (effect.Comp.EndEffectTime >= newEndTime)
                return;
        }

        SetStatusEffectEndTime(effect, newEndTime);
    }

    private void AddStatusEffectTime(Entity<StatusEffectComponent?> effect, TimeSpan delta)
    {
        if (!_effectQuery.Resolve(effect, ref effect.Comp))
            return;

        // It's already infinitely long can't add or subtract from infinity...
        if (effect.Comp.EndEffectTime is null)
            return;

        // Add to the current end effect time, if we're here we should have one set already, and if it's null it's probably infinite.
        SetStatusEffectEndTime((effect, effect.Comp), effect.Comp.EndEffectTime.Value + delta);
    }

    private void SetStatusEffectEndTime(Entity<StatusEffectComponent?> ent, TimeSpan? endTime)
    {
        if (!_effectQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.EndEffectTime == endTime)
            return;

        ent.Comp.EndEffectTime = endTime;

        if (ent.Comp.AppliedTo is not { } appliedTo)
            return; // Not much we can do!

        var ev = new StatusEffectEndTimeUpdatedEvent(appliedTo, endTime);
        RaiseLocalEvent(ent, ref ev);

        Dirty(ent);
    }
}

/// <summary>
/// Calls on effect entity, when a status effect is applied.
/// </summary>
[ByRefEvent]
public readonly record struct StatusEffectAppliedEvent(EntityUid Target);

/// <summary>
/// Calls on effect entity, when a status effect is removed.
/// </summary>
[ByRefEvent]
public readonly record struct StatusEffectRemovedEvent(EntityUid Target);

/// <summary>
/// Raised on an entity before a status effect is added to determine if adding it should be cancelled.
/// </summary>
[ByRefEvent]
public record struct BeforeStatusEffectAddedEvent(EntProtoId Effect, bool Cancelled = false);

/// <summary>
/// Raised on an effect entity when its <see cref="StatusEffectComponent.EndEffectTime"/> is updated in any way.
/// </summary>
/// <param name="Target">The entity the effect is attached to.</param>
/// <param name="EndTime">The new end time of the status effect, included for convenience.</param>
[ByRefEvent]
public record struct StatusEffectEndTimeUpdatedEvent(EntityUid Target, TimeSpan? EndTime);
