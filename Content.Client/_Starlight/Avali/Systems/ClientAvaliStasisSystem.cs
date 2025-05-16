using Content.Shared.Popups;
using Content.Shared.Starlight.Avali.Components;
using Content.Shared.Starlight.Avali.Events;
using Content.Shared.Starlight.Avali.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Starlight.Avali.Systems;

/// <summary>
/// Client-side system that handles visual and audio effects for Avali stasis.
/// </summary>
public sealed class ClientAvaliStasisSystem : SharedAvaliStasisSystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityUid? _continuousEffect;
    private EntityUid? _enterEffect;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimedDespawnComponent, ComponentShutdown>(OnTimedDespawnShutdown);
        SubscribeNetworkEvent<AvaliStasisAnimationEvent>(OnStasisAnimation);
    }

    private void OnStasisAnimation(AvaliStasisAnimationEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var entity = GetEntity(ev.Entity);
        if (!TryComp<AvaliStasisComponent>(entity, out var comp))
            return;

        switch (ev.AnimationType)
        {
            case AvaliStasisAnimationType.Enter:
                _popupSystem.PopupEntity(Loc.GetString("avali-stasis-entering"), entity, PopupType.Medium);
                StasisEnterAnimation(entity, comp);
                _continuousEffect = entity;
                break;
            case AvaliStasisAnimationType.Exit:
                _popupSystem.PopupEntity(Loc.GetString("avali-stasis-exiting"), entity, PopupType.Medium);
                StasisExitAnimation(entity, comp);
                EndStasisContinuousAnimation();
                break;
        }
    }

    private void OnTimedDespawnShutdown(EntityUid uid, TimedDespawnComponent component, ComponentShutdown args)
    {
        if (uid != _enterEffect)
            return;

        _enterEffect = null;
        if (_continuousEffect != null && TryComp<AvaliStasisComponent>(_continuousEffect, out var comp))
        {
            StartStasisContinuousAnimation(_continuousEffect.Value, comp);
        }
    }

    private void StasisEnterAnimation(EntityUid uid, AvaliStasisComponent comp)
    {
        EnsureComp<TransformComponent>(uid, out var xform);
        var effectEnt = SpawnAttachedTo(comp.StasisEnterEffect, xform.Coordinates);
        _xformSystem.SetParent(effectEnt, uid);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);
        despawnEffectEntComp.Lifetime = comp.StasisEnterEffectLifetime;
        if (TryComp<SpriteComponent>(effectEnt, out var sprite))
        {
            sprite.DrawDepth = (int) DrawDepth.Effects;
        }
        _audioSystem.PlayPvs(comp.StasisEnterSound, effectEnt);
        _enterEffect = effectEnt;
    }

    private void StasisExitAnimation(EntityUid uid, AvaliStasisComponent comp)
    {
        EnsureComp<TransformComponent>(uid, out var xform);
        var effectEnt = SpawnAttachedTo(comp.StasisExitEffect, xform.Coordinates);
        _xformSystem.SetParent(effectEnt, uid);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);
        despawnEffectEntComp.Lifetime = comp.StasisExitEffectLifetime;
        if (TryComp<SpriteComponent>(effectEnt, out var sprite))
        {
            sprite.DrawDepth = (int) DrawDepth.Effects;
        }
        _audioSystem.PlayPvs(comp.StasisExitSound, effectEnt);
    }

    private void StartStasisContinuousAnimation(EntityUid uid, AvaliStasisComponent comp)
    {
        EnsureComp<TransformComponent>(uid, out var xform);
        var effectEnt = SpawnAttachedTo(comp.StasisContinuousEffect, xform.Coordinates);
        _xformSystem.SetParent(effectEnt, uid);

        // Remove any auto-despawn components that might be on the effect
        RemComp<TimedDespawnComponent>(effectEnt);

        if (TryComp<SpriteComponent>(effectEnt, out var sprite))
        {
            sprite.DrawDepth = (int) DrawDepth.Effects;
        }

        _continuousEffect = effectEnt;
    }

    private void EndStasisContinuousAnimation()
    {
        if (_continuousEffect != null)
        {
            QueueDel(_continuousEffect.Value);
            _continuousEffect = null;
        }
    }
}