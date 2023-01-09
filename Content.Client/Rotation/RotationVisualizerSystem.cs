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

        if (!AppearanceSystem.TryGetData<RotationState>(uid, RotationVisuals.RotationState, out var state, args.Component) ||
            args.Sprite == null)
        {
            return;
        }

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

    /// <summary>
    ///     Rotates a sprite between two animated keyframes given a certain time.
    /// </summary>
    public void AnimateSpriteRotation(SpriteComponent spriteComp, Angle rotation, float animationTime)
    {
        if (spriteComp.Rotation.Equals(rotation))
        {
            return;
        }

        var animationComp = EnsureComp<AnimationPlayerComponent>(spriteComp.Owner);
        const string animationKey = "rotate";
        // Stop the current rotate animation and then start a new one
        if (AnimationSystem.HasRunningAnimation(animationComp, animationKey))
        {
            AnimationSystem.Stop(animationComp, animationKey);
        }
        spriteComp.Rotation = rotation;

        var animation = new Animation
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
                        new AnimationTrackProperty.KeyFrame(spriteComp.Rotation, 0),
                        new AnimationTrackProperty.KeyFrame(rotation, animationTime)
                    }
                }
            }
        };

        AnimationSystem.Play(animationComp, animation, animationKey);
    }
}
