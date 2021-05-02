using System;
using Content.Shared.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.Components.Strap;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Buckle
{
    /// <summary>
    /// This component is used to change the appearance of **mobs** when they're buckled.
    /// To change an object-on-which-you-can-buckle-a-mob's appearance, see <see cref="StrapVisualizer"/>
    /// </summary>
    [UsedImplicitly]
    public class BuckleVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            if (!component.TryGetData<bool>(BuckleVisuals.Buckled, out var buckled) ||
                !buckled)
            {
                return;
            }

            if (!component.TryGetData<int>(StrapVisuals.RotationAngle, out var angle))
            {
                return;
            }

            SetRotation(component, Angle.FromDegrees(angle));
        }

        private void SetRotation(AppearanceComponent component, Angle rotation)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!sprite.Owner.TryGetComponent(out AnimationPlayerComponent? animation))
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
