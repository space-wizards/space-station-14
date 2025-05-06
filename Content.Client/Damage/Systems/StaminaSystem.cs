using Content.Client.Stunnable;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;

namespace Content.Client.Damage.Systems;

public sealed partial class StaminaSystem : SharedStaminaSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    private const string StaminaAnimationKey = "stamina";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaminaComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<ActiveStaminaComponent, ComponentShutdown>(OnActiveStaminaShutdown);
        SubscribeLocalEvent<StaminaComponent, StunAnimationEvent>(OnStunAnimation);
        SubscribeLocalEvent<StaminaComponent, StunSystem.StunAnimationEndEvent>(OnStunAnimationEnd);
    }

    protected override void UpdateStaminaVisuals(Entity<StaminaComponent> entity)
    {
        base.UpdateStaminaVisuals(entity);

        TryStartAnimation(entity);
    }

    private void OnActiveStaminaShutdown(Entity<ActiveStaminaComponent> entity, ref ComponentShutdown args)
    {
        // If we don't have active stamina, we shouldn't have stamina damage. If the update loop can trust it we can trust it.
        if (!TryComp<StaminaComponent>(entity, out var stamina))
            return;

        StopAnimation((entity, stamina));
    }

    private void OnStunAnimation(Entity<StaminaComponent> entity, ref StunAnimationEvent args)
    {
        StopAnimation(entity);
    }

    protected override void OnShutdown(Entity<StaminaComponent> entity, ref ComponentShutdown args)
    {
        base.OnShutdown(entity, ref args);

        StopAnimation(entity);
    }

    private void TryStartAnimation(Entity<StaminaComponent> entity)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        // If the animation is running, the system should update it accordingly
        // If we're below the threshold to animate, don't try to animate
        // If we're in stamcrit don't override it
        if (entity.Comp.AnimationThreshold > entity.Comp.StaminaDamage || _animation.HasRunningAnimation(entity, StaminaAnimationKey))
            return;

        entity.Comp.StartOffset = sprite.Offset;

        PlayAnimation(entity, sprite);
    }

    private void StopAnimation(Entity<StaminaComponent> entity, SpriteComponent? sprite = null)
    {
        if(!Resolve(entity, ref sprite))
            return;

        _animation.Stop(entity.Owner, StaminaAnimationKey);
        entity.Comp.StartOffset = sprite.Offset;
    }

    private void OnStunAnimationEnd(Entity<StaminaComponent> entity, ref StunSystem.StunAnimationEndEvent args)
    {
        TryStartAnimation(entity);
    }

    private void OnAnimationCompleted(Entity<StaminaComponent> entity, ref AnimationCompletedEvent args)
    {
        if (args.Key != StaminaAnimationKey || !args.Finished || !TryComp<SpriteComponent>(entity, out var sprite))
            return;

        // stop looping if we're below the threshold
        if (entity.Comp.AnimationThreshold > entity.Comp.StaminaDamage)
        {
            _animation.Stop(entity.Owner, StaminaAnimationKey);
            sprite.Offset = entity.Comp.StartOffset;
            return;
        }

        if (!HasComp<AnimationPlayerComponent>(entity))
            return;

        PlayAnimation(entity, sprite);
    }

    private void PlayAnimation(Entity<StaminaComponent> entity, SpriteComponent sprite)
    {
        var step = Math.Clamp((entity.Comp.StaminaDamage - entity.Comp.AnimationThreshold) /
                              (entity.Comp.CritThreshold - entity.Comp.AnimationThreshold),
            0f,
            1f); // The things I do for project 0 warnings
        var frequency = entity.Comp.FrequencyMin + step * entity.Comp.FrequencyMod;
        var jitter = entity.Comp.JitterAmplitudeMin + step * entity.Comp.JitterAmplitudeMod;
        var breathing = entity.Comp.BreathingAmplitudeMin + step * entity.Comp.BreathingAmplitudeMod;

        _animation.Play(entity.Owner,
            _stun.GetFatigueAnimation(sprite,
                frequency,
                entity.Comp.Jitters,
                (jitter/2, jitter),
                (jitter/8, jitter/2),
                breathing,
                entity.Comp.StartOffset,
                ref entity.Comp.LastJitter),
            StaminaAnimationKey);
    }
}
