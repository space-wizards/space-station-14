using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Buckle
{
    public sealed class BuckleVisualsSystem : VisualizerSystem<BuckleVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, BuckleVisualsComponent component,
            ref AppearanceChangeEvent args)
        {

            if (!args.Component.TryGetData<bool>(BuckleVisuals.Buckled, out var buckled) || !buckled)
            {
                SetRotation(uid, Angle.FromDegrees(0));
                return;
            }

            if (!args.Component.TryGetData<int>(StrapVisuals.RotationAngle, out var angle))
            {
                return;
            }

            SetRotation(uid, Angle.FromDegrees(angle));
        }

        private void SetRotation(EntityUid uid, Angle rotation)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
            {
                return;
            }

            if (!TryComp(sprite.Owner, out AnimationPlayerComponent? animationPlayer))
            {
                sprite.Rotation = rotation;
                return;
            }

            if (animationPlayer.HasRunningAnimation("rotate"))
            {
                animationPlayer.Stop("rotate");
            }

            animationPlayer.Play(new Animation
            {
                Length = TimeSpan.FromSeconds(0.125),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Rotation),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(sprite.Rotation, 0),
                            new AnimationTrackProperty.KeyFrame(rotation, 0.125f)
                        }
                    }
                }
            }, "rotate");
        }


    }
}
