using System;
using Content.Shared.GameObjects.Components.Pointing;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Pointing
{
    [UsedImplicitly]
    public class RoguePointingArrowVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (component.TryGetData<double>(RoguePointingArrowVisuals.Rotation, out var degrees))
            {
                SetRotation(component, Angle.FromDegrees(degrees));
            }
        }

        private void SetRotation(AppearanceComponent component, Angle rotation)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!sprite.Owner.TryGetComponent(out AnimationPlayerComponent animation))
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
