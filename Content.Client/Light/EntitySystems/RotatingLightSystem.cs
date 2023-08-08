//using System;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Client.Light.Systems;

public sealed class RotatingLightSystem : EntitySystem
{
    private const float DegreesPerSecond = 90;
    private static Animation Animation => new()
    {
        Length = TimeSpan.FromSeconds(360f / DegreesPerSecond),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty
            {
                ComponentType = typeof(PointLightComponent),
                InterpolationMode = AnimationInterpolationMode.Linear,
                Property = nameof(PointLightComponent.Rotation),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(Angle.Zero, 0),
                    new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(120), 120f/DegreesPerSecond),
                    new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(240), 120f/DegreesPerSecond),
                    new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(360), 120f/DegreesPerSecond)
                }
            }
        }
    };

    private const string AnimKey = "rotating_light";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RotatingLightComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RotatingLightComponent, AnimationCompletedEvent>(OnAnimationComplete);
    }

    private void OnStartup(EntityUid uid, RotatingLightComponent component, ComponentStartup args)
    {
        PlayAnimation(uid, component);
    }

    private void OnAnimationComplete(EntityUid uid, RotatingLightComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<AnimationPlayerComponent>(uid, out var player)) return;

        player.Play(Animation, AnimKey);
    }

    private void PlayAnimation(EntityUid uid, RotatingLightComponent component)
    {
        var player = EnsureComp<AnimationPlayerComponent>(uid);
        if (!player.HasRunningAnimation(AnimKey))
            player.Play(Animation, AnimKey);
    }
}
