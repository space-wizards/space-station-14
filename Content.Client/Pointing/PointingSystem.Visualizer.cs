using Content.Shared.Pointing;
using Content.Shared.Pointing.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using System.Numerics;

namespace Content.Client.Pointing;

public sealed partial class PointingSystem : SharedPointingSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private TransformSystem _transformSystem = default!;

    private static readonly Vector2 PointingOffset = new(0, 0.25f);
    private const string PointingAnimationKey = "pointingarrow";

    public void InitializeVisualizer()
    {
        SubscribeLocalEvent<PointingArrowComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnAnimationCompleted(EntityUid uid, PointingArrowComponent component, AnimationCompletedEvent args)
    {
        if (args.Key == PointingAnimationKey)
            _animationPlayer.Stop(uid, PointingAnimationKey);
    }

    private void BeginPointAnimation(EntityUid uid, Vector2 startPosition)
    {
        if (_animationPlayer.HasRunningAnimation(uid, PointingAnimationKey))
            return;

        startPosition = new Angle(_eyeManager.CurrentEye.Rotation + _transformSystem.GetWorldRotation(uid)).RotateVec(startPosition);

        var animation = new Animation
        {
            Length = PointDuration,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        // We pad here to prevent improper looping and tighten the overshoot, just a touch
                        new AnimationTrackProperty.KeyFrame(startPosition, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startPosition, PointingOffset, 0.9f), PointKeyTimeMove),
                        new AnimationTrackProperty.KeyFrame(PointingOffset, PointKeyTimeMove),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, PointKeyTimeMove),
                        new AnimationTrackProperty.KeyFrame(PointingOffset, PointKeyTimeHover),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, PointKeyTimeHover),
                        new AnimationTrackProperty.KeyFrame(PointingOffset, PointKeyTimeHover),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, PointKeyTimeHover),
                        new AnimationTrackProperty.KeyFrame(PointingOffset, PointKeyTimeHover),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, PointKeyTimeHover),
                        new AnimationTrackProperty.KeyFrame(PointingOffset, PointKeyTimeHover),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, PointKeyTimeHover),
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, PointingAnimationKey);
    }
}
