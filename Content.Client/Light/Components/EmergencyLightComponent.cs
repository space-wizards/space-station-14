using System;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public class EmergencyLightComponent : Component
    {
        public override string Name => "EmergencyLight";

        /// <inheritdoc/>
        protected override void Startup()
        {
            base.Startup();

            var animation = new Animation
            {
                Length = TimeSpan.FromSeconds(4),
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
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(1080), 4)
                        }
                    }
                }
            };

            var playerComponent = Owner.EnsureComponent<AnimationPlayerComponent>();
            playerComponent.Play(animation, "emergency");

            playerComponent.AnimationCompleted += s => playerComponent.Play(animation, s);
        }
    }
}
