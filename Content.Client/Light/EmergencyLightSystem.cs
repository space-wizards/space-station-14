using System;
using Content.Client.Light.Components;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Client.Light
{
    public sealed class EmergencyLightSystem : SharedEmergencyLightSystem
    {
        private const float DegreesPerSecond = 90;
        private static Animation Animation =>
            new()
            {
                Length = TimeSpan.FromSeconds(360f/ DegreesPerSecond),
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
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(120), 120f/DegreesPerSecond),
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(240), 120f/DegreesPerSecond),
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(360), 120f/DegreesPerSecond)
                        }
                    }
                }
            };

        private const string AnimKey = "emergency";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmergencyLightComponent, ComponentStartup>(HandleStartup);
            SubscribeLocalEvent<EmergencyLightComponent, AnimationCompletedEvent>(HandleAnimationComplete);
            SubscribeLocalEvent<EmergencyLightComponent, ComponentHandleState>(HandleCompState);
        }

        private void HandleCompState(EntityUid uid, EmergencyLightComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not EmergencyLightComponentState state) return;

            if (component.Enabled == state.Enabled) return;

            var playerComponent = component.Owner.EnsureComponent<AnimationPlayerComponent>();

            component.Enabled = state.Enabled;

            if (component.Enabled && !playerComponent.HasRunningAnimation(AnimKey))
                playerComponent.Play(Animation, AnimKey);

            if (!component.Enabled)
                playerComponent.Stop(AnimKey);
        }

        private void HandleAnimationComplete(EntityUid uid, EmergencyLightComponent component, AnimationCompletedEvent args)
        {
            if (!component.Enabled ||
                !EntityManager.TryGetComponent<AnimationPlayerComponent>(uid, out var playerComponent)) return;

            playerComponent.Play(Animation, AnimKey);
        }

        private void HandleStartup(EntityUid uid, EmergencyLightComponent component, ComponentStartup args)
        {
            PlayAnimation(component);
        }

        private void PlayAnimation(EmergencyLightComponent component)
        {
            if (!component.Enabled) return;

            var playerComponent = component.Owner.EnsureComponent<AnimationPlayerComponent>();

            if (!playerComponent.HasRunningAnimation(AnimKey))
                playerComponent.Play(Animation, AnimKey);
        }
    }
}
