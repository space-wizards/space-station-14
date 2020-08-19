using System;
using Content.Shared.GameObjects.Components.Power;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public class EmergencyLightComponent : SharedEmergencyLightComponent
    {
        [ViewVariables]
        public EmergencyLightState State { get; set; }

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

        public override void HandleComponentState(ComponentState currentState, ComponentState nextState)
        {
            if (currentState == null) return;
            State = ((EmergencyLightComponentState) currentState).State;
        }
    }
}
