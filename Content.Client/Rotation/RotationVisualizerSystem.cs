using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Content.Shared.Rotation;
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
        var entMan = IoCManager.Resolve<IEntityManager>();

        if (sprite.Rotation.Equals(rotation))
        {
            return;
        }

        if (!entMan.TryGetComponent(sprite.Owner, out AnimationPlayerComponent? animation))
        {
            sprite.Rotation = rotation;
            return;
        }

        if (animation.HasRunningAnimation("rotate"))
        {
            animation.Stop("rotate");
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
        }, "rotate");
    }
}
