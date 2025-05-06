using Content.Shared.Alert;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.StatusEffectNew;

public abstract partial class SharedStatusEffectNewSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<StatusEffectContainerComponent> _containerQuery;
    private EntityQuery<StatusEffectNewComponent> _effectQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectNewComponent, StatusEffectApplied>(OnStatusEffectApplied);
        SubscribeLocalEvent<StatusEffectNewComponent, StatusEffectRemoved>(OnStatusEffectRemoved);

        _containerQuery = GetEntityQuery<StatusEffectContainerComponent>();
        _effectQuery = GetEntityQuery<StatusEffectNewComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StatusEffectNewComponent>();
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

    private void EditStatusEffectTime(EntityUid effect, TimeSpan delta)
    {
        if (!_effectQuery.TryComp(effect, out var effectComp))
            return;

        if (effectComp.AppliedTo is null)
            return;

        if (effectComp.Alert is not null)
        {
            effectComp.EndEffectTime += delta;
            _alerts.ShowAlert(
                effectComp.AppliedTo.Value,
                effectComp.Alert.Value,
                cooldown: effectComp.EndEffectTime is null ? null : (_timing.CurTime, effectComp.EndEffectTime.Value));
        }
    }

    private void SetStatusEffectTime(EntityUid effect, TimeSpan duration)
    {
        if (!_effectQuery.TryComp(effect, out var effectComp))
            return;

        if (effectComp.AppliedTo is null)
            return;

        if (effectComp.Alert is not null)
        {
            effectComp.EndEffectTime = _timing.CurTime + duration;
            _alerts.ShowAlert(
                effectComp.AppliedTo.Value,
                effectComp.Alert.Value,
                cooldown: effectComp.EndEffectTime is null ? null : (_timing.CurTime, effectComp.EndEffectTime.Value));
        }
    }

    private void OnStatusEffectApplied(Entity<StatusEffectNewComponent> ent, ref StatusEffectApplied args)
    {
        if (ent.Comp.AppliedTo is null)
            return;

        if (ent.Comp.Alert is not null)
        {
            _alerts.ShowAlert(
                ent.Comp.AppliedTo.Value,
                ent.Comp.Alert.Value,
                cooldown: ent.Comp.EndEffectTime is null ? null : (_timing.CurTime, ent.Comp.EndEffectTime.Value));
        }

        if (_net.IsServer)
            EntityManager.AddComponents(args.Target, ent.Comp.Components);
    }

    private void OnStatusEffectRemoved(Entity<StatusEffectNewComponent> ent, ref StatusEffectRemoved args)
    {
        if (ent.Comp.AppliedTo is null)
            return;

        if (ent.Comp.Alert is not null)
        {
            _alerts.ClearAlert(ent.Comp.AppliedTo.Value, ent.Comp.Alert.Value);
        }

        if (_net.IsServer)
            EntityManager.RemoveComponents(args.Target, ent.Comp.Components);
    }
}

/// <summary>
/// Calls on both effect entity and target entity, when a status effect is applied.
/// </summary>
[ByRefEvent]
public readonly record struct StatusEffectApplied(EntityUid Target, Entity<StatusEffectNewComponent> Effect);

/// <summary>
/// Calls on both effect entity and target entity, when a status effect is removed.
/// </summary>
[ByRefEvent]
public readonly record struct StatusEffectRemoved(EntityUid Target, Entity<StatusEffectNewComponent> Effect);

/// <summary>
/// Raised on an entity before a status effect is added to determine if adding it should be cancelled.
/// </summary>
[ByRefEvent]
public record struct BeforeStatusEffectAddedEvent(EntProtoId Effect, bool Cancelled = false);
