using System;
using Content.Shared.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class RadiationCollectorVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string AnimationKey = "radiationcollector_animation";

        private Animation ActivateAnimation;
        private Animation DeactiveAnimation;

        void ISerializationHooks.AfterDeserialization()
        {
            ActivateAnimation = new Animation {Length = TimeSpan.FromSeconds(0.8f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                ActivateAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = RadiationCollectorVisualLayers.Main;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("ca_active", 0f));

                /*var sound = new AnimationTrackPlaySound();
                CloseAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(closeSound, 0));*/
            }

            DeactiveAnimation = new Animation {Length = TimeSpan.FromSeconds(0.8f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                DeactiveAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = RadiationCollectorVisualLayers.Main;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("ca_deactive", 0f));

                /*var sound = new AnimationTrackPlaySound();
                CloseAnimation.AnimationTracks.Add(sound);
                sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(closeSound, 0));*/
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            if (!entity.HasComponent<AnimationPlayerComponent>())
            {
                entity.AddComponent<AnimationPlayerComponent>();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
            if (!component.Owner.TryGetComponent<AnimationPlayerComponent>(out var animPlayer)) return;
            if (!component.TryGetData(RadiationCollectorVisuals.VisualState, out RadiationCollectorVisualState state))
            {
                state = RadiationCollectorVisualState.Deactive;
            }

            switch (state)
            {
                case RadiationCollectorVisualState.Active:
                    sprite.LayerSetState(RadiationCollectorVisualLayers.Main, "ca_on");
                    break;
                case RadiationCollectorVisualState.Activating:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(ActivateAnimation, AnimationKey);
                        animPlayer.AnimationCompleted += _ =>
                            component.SetData(RadiationCollectorVisuals.VisualState,
                                RadiationCollectorVisualState.Active);
                    }
                    break;
                case RadiationCollectorVisualState.Deactivating:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(DeactiveAnimation, AnimationKey);
                        animPlayer.AnimationCompleted += _ =>
                            component.SetData(RadiationCollectorVisuals.VisualState,
                                RadiationCollectorVisualState.Deactive);
                    }
                    break;
                case RadiationCollectorVisualState.Deactive:
                    sprite.LayerSetState(RadiationCollectorVisualLayers.Main, "ca_off");
                    break;
            }
        }
    }
    public enum RadiationCollectorVisualLayers : byte
    {
        Main
    }
}
