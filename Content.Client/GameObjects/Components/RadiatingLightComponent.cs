using System;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public class RadiatingLightComponent : Component
    {
        public override string Name => "RadiatingLight";

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
                        Property = nameof(PointLightComponent.Radius),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(3.0f, 0),
                            new AnimationTrackProperty.KeyFrame(2.0f, 1),
                            new AnimationTrackProperty.KeyFrame(3.0f, 2)
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
