using System;
using Content.Shared.Rotation;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Rotation
{
    [UsedImplicitly]
    public sealed class RotationVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            // if TryGet fails, state defaults to RotationState.Vertical.
            component.TryGetData<RotationState>(RotationVisuals.RotationState, out var state);

            switch (state)
            {
                case RotationState.Vertical:
                    SetRotation(component, 0);
                    break;
                case RotationState.Horizontal:
                    SetRotation(component, Angle.FromDegrees(90));
                    break;
            }
        }

        private void SetRotation(AppearanceComponent component, Angle rotation)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var sprite = entMan.GetComponent<ISpriteComponent>(component.Owner);

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
                Length = TimeSpan.FromSeconds(0.125),
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
                            new AnimationTrackProperty.KeyFrame(rotation, 0.125f)
                        }
                    }
                }
            }, "rotate");
        }
    }
}
