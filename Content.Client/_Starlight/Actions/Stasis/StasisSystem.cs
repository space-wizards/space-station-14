using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Content.Shared._Starlight.Actions.Stasis;

namespace Content.Client._Starlight.Actions.Stasis;

/// <summary>
/// Client-side system that handles visual and audio effects for stasis.
/// </summary>
public sealed class StasisSystem : SharedStasisSystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeNetworkEvent<StasisAnimationEvent>(OnStasisAnimation);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Periodic cleanup of orphaned effects
        CleanupOrphanedEffects();
        
        // Periodic visibility state check to ensure consistency
        CheckVisibilityStates();
        
        // Periodic continuous effect check to ensure PVS synchronization
        CheckContinuousEffects();
    }

    private void CleanupOrphanedEffects()
    {
        var query = AllEntityQuery<StasisComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Skip if the entity is being deleted
            if (EntityManager.IsQueuedForDeletion(uid))
                continue;

            // If the entity is no longer in stasis but has a continuous effect, clean it up
            if (!comp.IsInStasis && comp.ClientContinuousEffectEntity != null)
            {
                if (Exists(comp.ClientContinuousEffectEntity.Value))
                {
                    QueueDel(comp.ClientContinuousEffectEntity.Value);
                }
                comp.ClientContinuousEffectEntity = null;
                Dirty(uid, comp);
            }
            // If the continuous effect entity no longer exists, clear the reference
            else if (comp.ClientContinuousEffectEntity != null && !Exists(comp.ClientContinuousEffectEntity.Value))
            {
                comp.ClientContinuousEffectEntity = null;
                Dirty(uid, comp);
            }
            
            // Clean up orphaned enter effects
            if (comp.ClientEnterEffectEntity != null && !Exists(comp.ClientEnterEffectEntity.Value))
            {
                comp.ClientEnterEffectEntity = null;
                Dirty(uid, comp);
            }
        }
    }

    private void OnStasisAnimation(StasisAnimationEvent ev)
    {
        // This is a hack to prevent the animation from playing multiple times.
        if (!_timing.IsFirstTimePredicted)
            return;

        var entity = GetEntity(ev.Entity);
        if (!TryComp<StasisComponent>(entity, out var comp))
            return;

        // We react to a specific animation event, and then play the appropriate animation.
        switch (ev.AnimationType)
        {
            case StasisAnimationType.Prepare:
                // Show a popup to the player.
                _popupSystem.PopupEntity(Loc.GetString("stasis-entering"), entity, PopupType.Medium);
                // Play the prepare animation.
                StasisPrepareAnimation(entity, comp);
                break;
            case StasisAnimationType.Enter:
                // Play the enter animation.
                StasisEnterAnimation(entity, comp);
                break;
            case StasisAnimationType.Exit:
                // Show a popup to the player.
                _popupSystem.PopupEntity(Loc.GetString("stasis-exiting"), entity, PopupType.Medium);
                // Play the exit animation.
                StasisExitAnimation(entity, comp);
                // End the continuous animation.
                EndStasisContinuousAnimation(entity, comp);
                break;
        }
    }

    private void StasisPrepareAnimation(EntityUid uid, StasisComponent comp)
    {
        // Safety check to ensure the entity still exists
        if (!Exists(uid))
            return;

        EnsureComp<TransformComponent>(uid, out var xform);
        var effectEnt = SpawnAttachedTo(comp.StasisEnterEffect, xform.Coordinates);
        _xformSystem.SetParent(effectEnt, uid);
        RemComp<TimedDespawnComponent>(effectEnt);
        if (TryComp<SpriteComponent>(effectEnt, out var sprite))
        {
            // Set it to be over the parent entity.
            sprite.DrawDepth = (int)DrawDepth.Effects;
            // Prevent it from rotating.
            sprite.NoRotation = true;
            sprite.Visible = TryComp<SpriteComponent>(uid, out var parentSprite) && parentSprite.Visible;
        }

        // Play the sound effect.
        _audioSystem.PlayPvs(comp.StasisEnterSound, effectEnt);
        comp.ClientEnterEffectEntity = effectEnt;
        Dirty(uid, comp);
    }

    private void StasisEnterAnimation(EntityUid uid, StasisComponent comp)
    {
        // Safety check to ensure the entity still exists
        if (!Exists(uid))
            return;
            
        // Start the continuous animation.
        StartStasisContinuousAnimation(uid, comp);
        // Delete the prepare animation.
        if (comp.ClientEnterEffectEntity != null)
        {
            QueueDel(comp.ClientEnterEffectEntity.Value);
            comp.ClientEnterEffectEntity = null;
            Dirty(uid, comp);
        }
        
        // Update visibility based on server state
        UpdateEntityVisibility(uid, comp);
    }

    private void StasisExitAnimation(EntityUid uid, StasisComponent comp)
    {
        // Safety check to ensure the entity still exists
        if (!Exists(uid))
            return;

        EnsureComp<TransformComponent>(uid, out var xform);
        var effectEnt = SpawnAttachedTo(comp.StasisExitEffect, xform.Coordinates);
        _xformSystem.SetParent(effectEnt, uid);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);
        despawnEffectEntComp.Lifetime = comp.StasisExitEffectLifetime;
        if (TryComp<SpriteComponent>(effectEnt, out var sprite))
        {
            // Set it to be over the parent entity.
            sprite.DrawDepth = (int)DrawDepth.Effects;
            // Prevent it from rotating.
            sprite.NoRotation = true;
            sprite.Visible = TryComp<SpriteComponent>(uid, out var parentSprite) && parentSprite.Visible;
        }

        // Play the sound effect.
        _audioSystem.PlayPvs(comp.StasisExitSound, effectEnt);

        // End the continuous animation.
        EndStasisContinuousAnimation(uid, comp);
        
        // Update visibility based on server state
        UpdateEntityVisibility(uid, comp);
    }

    private void StartStasisContinuousAnimation(EntityUid uid, StasisComponent comp)
    {
        // Safety check to ensure the entity still exists
        if (!Exists(uid))
            return;

        // Clean up any existing continuous effect for this entity first
        EndStasisContinuousAnimation(uid, comp);

        EnsureComp<TransformComponent>(uid, out var xform);
        var effectEnt = SpawnAttachedTo(comp.StasisContinuousEffect, xform.Coordinates);
        _xformSystem.SetParent(effectEnt, uid);

        // Remove any auto-despawn components that might be on the effect
        RemComp<TimedDespawnComponent>(effectEnt);

        if (TryComp<SpriteComponent>(effectEnt, out var sprite))
        {
            // Set it to be over the parent entity.
            sprite.DrawDepth = (int)DrawDepth.Effects;
            // Prevent it from rotating.
            sprite.NoRotation = true;
            // Make it visible if the parent entity is visible.
            sprite.Visible = TryComp<SpriteComponent>(uid, out var parentSprite) && parentSprite.Visible;
        }

        // Store the continuous effect in the component
        comp.ClientContinuousEffectEntity = effectEnt;
        Dirty(uid, comp);
    }

    private void EndStasisContinuousAnimation(EntityUid uid, StasisComponent comp)
    {
        // If there's a continuous effect for this entity, delete it.
        if (comp.ClientContinuousEffectEntity != null)
        {
            // Validate that the effect entity still exists before trying to delete it
            if (Exists(comp.ClientContinuousEffectEntity.Value))
            {
                QueueDel(comp.ClientContinuousEffectEntity.Value);
            }
            comp.ClientContinuousEffectEntity = null;
            Dirty(uid, comp);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        
        // Clean up all continuous effects on shutdown
        var query = AllEntityQuery<StasisComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ClientContinuousEffectEntity != null && Exists(comp.ClientContinuousEffectEntity.Value))
            {
                QueueDel(comp.ClientContinuousEffectEntity.Value);
            }
            if (comp.ClientEnterEffectEntity != null && Exists(comp.ClientEnterEffectEntity.Value))
            {
                QueueDel(comp.ClientEnterEffectEntity.Value);
            }
        }
    }

    private void UpdateEntityVisibility(EntityUid uid, StasisComponent comp)
    {
        // Check if the entity still exists
        if (!Exists(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // Update visibility based on server state
        if (comp.IsVisible)
        {
            // Entity should be visible
            sprite.Color = sprite.Color.WithAlpha(1f);
        }
        else
        {
            // Entity should be invisible
            sprite.Color = sprite.Color.WithAlpha(0f);
        }
    }

    private void CheckVisibilityStates()
    {
        var query = AllEntityQuery<StasisComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            // Skip if the entity is being deleted
            if (EntityManager.IsQueuedForDeletion(uid))
                continue;

            // Update visibility based on server state
            UpdateEntityVisibility(uid, comp);
        }
    }

    private void CheckContinuousEffects()
    {
        var query = AllEntityQuery<StasisComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Skip if the entity is being deleted
            if (EntityManager.IsQueuedForDeletion(uid))
                continue;

            // If entity is in stasis but doesn't have the continuous effect, reapply it
            // This handles cases where players enter PVS range after the animation events were sent
            if (comp.IsInStasis && comp.ClientContinuousEffectEntity == null)
            {
                StartStasisContinuousAnimation(uid, comp);
            }
        }
    }
}