using Content.Shared.Rotation;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Rotation;

public sealed class RotationVisualizerSystem : VisualizerSystem<RotationVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RotationVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite != null)
        {
            // if TryGetData fails, state defaults to RotationState.Vertical.
            args.Component.TryGetData<RotationState>(RotationVisuals.RotationState, out var state);

            switch (state)
            {
                case RotationState.Vertical:
                    AnimateSpriteRotation(args.Sprite, component.VerticalRotation, component.AnimationTime);
                    break;
                case RotationState.Horizontal:
                    AnimateSpriteRotation(args.Sprite, component.HorizontalRotation, component.AnimationTime);
                    break;
            }
        }
    }

    /// <summary>
    ///     Rotates a sprite between two animated keyframes given a certain time.
    /// </summary>
    public void AnimateSpriteRotation(SpriteComponent sprite, Angle rotation, float animationTime)
    {
        if (sprite.Rotation.Equals(rotation))
        {
            return;
        }

        if (!TryComp<AnimationPlayerComponent>(sprite.Owner, out var animation))
        {
            sprite.Rotation = rotation;
            return;
        }

        const string animationKey = "rotate";
        if (animation.HasRunningAnimation(animationKey))
        {
            animation.Stop(animationKey);
        }

        animation.Play(new Animation
        {
            Length = TimeSpan.FromSeconds(animationTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(ISpriteComponent),
                    Property = nameof(ISpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation, 0),
                        new AnimationTrackProperty.KeyFrame(rotation, animationTime)
                    }
                }
            }
        }, animationKey);
    }
}
