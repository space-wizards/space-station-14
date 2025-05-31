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
        SubscribeNetworkEvent<AvaliStasisAnimationEvent>(OnStasisAnimation);
    }

    private void OnStasisAnimation(AvaliStasisAnimationEvent ev)
    {
        // This is a hack to prevent the animation from playing multiple times.
        if (!_timing.IsFirstTimePredicted)
            return;

        var entity = GetEntity(ev.Entity);
        if (!TryComp<AvaliStasisComponent>(entity, out var comp))
            return;

        // We react to a specific animation event, and then play the appropriate animation.
        switch (ev.AnimationType)
        {
            case AvaliStasisAnimationType.Prepare:
                // Show a popup to the player.
                _popupSystem.PopupEntity(Loc.GetString("avali-stasis-entering"), entity, PopupType.Medium);
                // Play the prepare animation.
                StasisPrepareAnimation(entity, comp);
                break;
            case AvaliStasisAnimationType.Enter:
                // Play the enter animation.
                StasisEnterAnimation(entity, comp);
                break;
            case AvaliStasisAnimationType.Exit:
                // Show a popup to the player.
                _popupSystem.PopupEntity(Loc.GetString("avali-stasis-exiting"), entity, PopupType.Medium);
                // Play the exit animation.
                StasisExitAnimation(entity, comp);
                // End the continuous animation.
                EndStasisContinuousAnimation();
                break;
        }
    }

    private void StasisPrepareAnimation(EntityUid uid, AvaliStasisComponent comp)
    {
        EnsureComp<TransformComponent>(uid, out var xform);
        var effectEnt = SpawnAttachedTo(comp.StasisEnterEffect, xform.Coordinates);
        _xformSystem.SetParent(effectEnt, uid);
        RemComp<TimedDespawnComponent>(effectEnt);
        if (TryComp<SpriteComponent>(effectEnt, out var sprite))
        {
            // Set it to be over the parent entity.
            sprite.DrawDepth = (int) DrawDepth.Effects;
            // Prevent it from rotating.
            sprite.NoRotation = true;
            sprite.Visible = TryComp<SpriteComponent>(uid, out var parentSprite) && parentSprite.Visible;
        }
        // Play the sound effect.
        _audioSystem.PlayPvs(comp.StasisEnterSound, effectEnt);
        _enterEffect = effectEnt;
    }
    
    private void StasisEnterAnimation(EntityUid uid, AvaliStasisComponent comp)
    {
        // Start the continuous animation.
        StartStasisContinuousAnimation(uid, comp);
        // Delete the prepare animation.
        if (_enterEffect != null)
        {
            QueueDel(_enterEffect.Value);
            _enterEffect = null;
        }
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
            // Set it to be over the parent entity.
            sprite.DrawDepth = (int) DrawDepth.Effects;
            // Prevent it from rotating.
            sprite.NoRotation = true;
            sprite.Visible = TryComp<SpriteComponent>(uid, out var parentSprite) && parentSprite.Visible;
        }
        // Play the sound effect.
        _audioSystem.PlayPvs(comp.StasisExitSound, effectEnt);

        // Restore entity visibility
        if (TryComp<SpriteComponent>(uid, out var entitySprite))
        {
            entitySprite.Color = entitySprite.Color.WithAlpha(1f);
        }
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
            // Set it to be over the parent entity.
            sprite.DrawDepth = (int) DrawDepth.Effects;
            // Prevent it from rotating.
            sprite.NoRotation = true;
            // Make it visible if the parent entity is visible.
            sprite.Visible = TryComp<SpriteComponent>(uid, out var parentSprite) && parentSprite.Visible;
        }

        // Make the entity fully transparent, to hide it from being seen while stasis is active.
        if (TryComp<SpriteComponent>(uid, out var entitySprite))
        {
            entitySprite.Color = entitySprite.Color.WithAlpha(0f);
        }

        // Set the continuous effect to the effect entity.
        _continuousEffect = effectEnt;
    }

    private void EndStasisContinuousAnimation()
    {
        // If the continuous effect is set, delete it.
        if (_continuousEffect != null)
        {
            QueueDel(_continuousEffect.Value);
            _continuousEffect = null;
        }
    }
}