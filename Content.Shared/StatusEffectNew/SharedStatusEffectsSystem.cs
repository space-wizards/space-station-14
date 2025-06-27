using Content.Shared.Alert;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// This system controls status effects, their lifetime, and provides an API for adding them to entities,
/// removing them from entities, or getting information about current effects on entities.
/// </summary>
public abstract partial class SharedStatusEffectsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<StatusEffectContainerComponent> _containerQuery;
    private EntityQuery<StatusEffectComponent> _effectQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRelay();

        SubscribeLocalEvent<StatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<StatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);

        SubscribeLocalEvent<StatusEffectContainerComponent, ComponentGetState>(OnGetState);

        _containerQuery = GetEntityQuery<StatusEffectContainerComponent>();
        _effectQuery = GetEntityQuery<StatusEffectComponent>();
    }

    private void OnGetState(Entity<StatusEffectContainerComponent> ent, ref ComponentGetState args)
    {
        args.State = new StatusEffectContainerComponentState(GetNetEntitySet(ent.Comp.ActiveStatusEffects));
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

    private void AddStatusEffectTime(EntityUid effect, TimeSpan delta)
    {
        if (!_effectQuery.TryComp(effect, out var effectComp))
            return;

        effectComp.EndEffectTime += delta;
        Dirty(effect, effectComp);

        if (effectComp is { AppliedTo: not null, Alert: not null })
        {
            (TimeSpan Start, TimeSpan End)? cooldown = effectComp.EndEffectTime is null
                ? null
                : (_timing.CurTime, effectComp.EndEffectTime.Value);
            _alerts.ShowAlert(
                effectComp.AppliedTo.Value,
                effectComp.Alert.Value,
                cooldown: cooldown
            );
        }
    }

    private void SetStatusEffectTime(EntityUid effect, TimeSpan duration)
    {
        if (!_effectQuery.TryComp(effect, out var effectComp))
            return;

        effectComp.EndEffectTime = _timing.CurTime + duration;
        Dirty(effect, effectComp);

        if (effectComp is { AppliedTo: not null, Alert: not null })
        {
            (TimeSpan, TimeSpan)? cooldown = effectComp.EndEffectTime is null
                ? null
                : (_timing.CurTime, effectComp.EndEffectTime.Value);
            _alerts.ShowAlert(
                effectComp.AppliedTo.Value,
                effectComp.Alert.Value,
                cooldown: cooldown
            );
        }
    }

    private void OnStatusEffectApplied(Entity<StatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (ent.Comp is { AppliedTo: not null, Alert: not null })
        {
            (TimeSpan, TimeSpan)? cooldown = ent.Comp.EndEffectTime is null
                ? null
                : (_timing.CurTime, ent.Comp.EndEffectTime.Value);
            _alerts.ShowAlert(
                ent.Comp.AppliedTo.Value,
                ent.Comp.Alert.Value,
                cooldown: cooldown
            );
        }
    }

    private void OnStatusEffectRemoved(Entity<StatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.AppliedTo is null)
            return;

        if (ent.Comp is { AppliedTo: not null, Alert: not null })
            _alerts.ClearAlert(ent.Comp.AppliedTo.Value, ent.Comp.Alert.Value);
    }

    private bool CanAddStatusEffect(EntityUid uid, EntProtoId effectProto)
    {
        if (!_proto.TryIndex(effectProto, out var effectProtoData))
            return false;

        if (!effectProtoData.TryGetComponent<StatusEffectComponent>(out var effectProtoComp, _compFactory))
            return false;

        if (!_whitelist.CheckBoth(uid, effectProtoComp.Blacklist, effectProtoComp.Whitelist))
            return false;

        var ev = new BeforeStatusEffectAddedEvent(effectProto);
        RaiseLocalEvent(uid, ref ev);

        if (ev.Cancelled)
            return false;

        return true;
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
