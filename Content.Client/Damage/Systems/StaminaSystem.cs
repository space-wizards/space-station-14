using Content.Client.Stunnable;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Damage.Systems;

public sealed partial class StaminaSystem : SharedStaminaSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    private const string StaminaAnimationKey = "stamina";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaminaComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<ActiveStaminaComponent, ComponentShutdown>(OnActiveStaminaShutdown);
        SubscribeLocalEvent<StaminaComponent, MobStateChangedEvent>(OnMobStateChanged);
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

    protected override void OnShutdown(Entity<StaminaComponent> entity, ref ComponentShutdown args)
    {
        base.OnShutdown(entity, ref args);

        StopAnimation(entity);
    }

    private void OnMobStateChanged(Entity<StaminaComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            StopAnimation(ent);
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

        // Don't animate if we're dead
        if (TryComp<MobStateComponent>(entity, out var state) && state.CurrentState == MobState.Dead)
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

    private void OnAnimationCompleted(Entity<StaminaComponent> entity, ref AnimationCompletedEvent args)
    {
        if (args.Key != StaminaAnimationKey || !args.Finished || !TryComp<SpriteComponent>(entity, out var sprite))
            return;

        // stop looping if we're below the threshold
        if (entity.Comp.AnimationThreshold > entity.Comp.StaminaDamage)
        {
            _animation.Stop(entity.Owner, StaminaAnimationKey);
            _sprite.SetOffset((entity, sprite), entity.Comp.StartOffset);
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
                (jitter * entity.Comp.JitterMinMaxX.Item1, jitter * entity.Comp.JitterMinMaxX.Item2),
                (jitter * entity.Comp.JitterMinMaxY.Item1, jitter * entity.Comp.JitterMinMaxY.Item2),
                breathing,
                entity.Comp.StartOffset,
                ref entity.Comp.LastJitter),
            StaminaAnimationKey);
    }
}
