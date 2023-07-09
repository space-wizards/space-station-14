using Content.Shared.Rotation;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Rotation;

public sealed class RotationVisualizerSystem : VisualizerSystem<RotationVisualsComponent>
{
    public void SetHorizontalAngle(EntityUid uid, Angle angle, RotationVisualsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.HorizontalRotation.Equals(angle))
            return;

        component.HorizontalRotation = angle;
        Dirty(component);
    }

    protected override void OnAppearanceChange(EntityUid uid, RotationVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null)
            return;

        // If not defined, defaults to standing.
        AppearanceSystem.TryGetData<RotationState>(uid, RotationVisuals.RotationState, out var state, args.Component);

        switch (state)
        {
            case RotationState.Vertical:
                AnimateSpriteRotation(uid, args.Sprite, component.VerticalRotation, component.AnimationTime);
                break;
            case RotationState.Horizontal:
                AnimateSpriteRotation(uid, args.Sprite, component.HorizontalRotation, component.AnimationTime);
                break;
        }
    }

    /// <summary>
    ///     Rotates a sprite between two animated keyframes given a certain time.
    /// </summary>
    public void AnimateSpriteRotation(EntityUid uid, SpriteComponent spriteComp, Angle rotation, float animationTime)
    {
        if (spriteComp.Rotation.Equals(rotation))
        {
            return;
        }

        var animationComp = EnsureComp<AnimationPlayerComponent>(uid);
        const string animationKey = "rotate";
        // Stop the current rotate animation and then start a new one
        if (AnimationSystem.HasRunningAnimation(animationComp, animationKey))
        {
            AnimationSystem.Stop(animationComp, animationKey);
        }

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(animationTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
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
