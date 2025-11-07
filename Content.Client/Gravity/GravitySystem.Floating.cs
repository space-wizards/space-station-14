using System.Numerics;
using Content.Shared.Gravity;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Robust.Shared.Animations;

namespace Content.Client.Gravity;

/// <summary>
/// Handles offsetting a sprite when there is no gravity.
/// </summary>
public sealed partial class GravitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;

    public void InitializeFloatingVisuals()
    {
        SubscribeLocalEvent<FloatingVisualsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<FloatingVisualsComponent, WeightlessnessChangedEvent>(OnWeightlessnessChanged);
        SubscribeLocalEvent<FloatingVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnComponentStartup(Entity<FloatingVisualsComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.IsFloating = IsWeightless(ent.Owner);

        if (ent.Comp.IsFloating)
            FloatAnimation(ent, ent.Comp.Offset, FloatingVisualsComponent.AnimationKey, ent.Comp.AnimationTime);
    }

    private void OnWeightlessnessChanged(Entity<FloatingVisualsComponent> entity, ref WeightlessnessChangedEvent args)
    {
        if (entity.Comp.IsFloating == args.Weightless)
            return;

        entity.Comp.IsFloating = args.Weightless;

        // If we started being weightless play the animation.
        // If we stopped being weightless do nothing so the animation can finish and then stops.
        if (entity.Comp.IsFloating)
            FloatAnimation(entity, entity.Comp.Offset, FloatingVisualsComponent.AnimationKey, entity.Comp.AnimationTime);
    }

    private void OnAnimationCompleted(EntityUid uid, FloatingVisualsComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != FloatingVisualsComponent.AnimationKey)
            return;

        // repeat the animation if still weightless
        FloatAnimation(uid, component.Offset, FloatingVisualsComponent.AnimationKey, component.AnimationTime, stop: !component.IsFloating);
    }

    /// <summary>
    /// Offsets a sprite with a linear interpolation animation
    /// </summary>
    public void FloatAnimation(EntityUid uid, Vector2 offset, string animationKey, float animationTime, bool stop = false)
    {
        if (stop)
        {
            _animationSystem.Stop(uid, animationKey);
            return;
        }

        var animation = new Animation
        {
            // We multiply by the number of extra keyframes to make time for them
            Length = TimeSpan.FromSeconds(animationTime * 2),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(offset, animationTime),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, animationTime),
                    }
                }
            }
        };

        if (!_animationSystem.HasRunningAnimation(uid, animationKey))
            _animationSystem.Play(uid, animation, animationKey);
    }
}
