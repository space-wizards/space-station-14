using System;
using Content.Shared.Singularity.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Client.Singularity.Visualizers
{
    [UsedImplicitly]
    public sealed class RadiationCollectorVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string AnimationKey = "radiationcollector_animation";

        private Animation ActivateAnimation = default!;
        private Animation DeactiveAnimation = default!;

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

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            IoCManager.Resolve<IEntityManager>().EnsureComponent<AnimationPlayerComponent>(entity);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite)) return;
            if (!entities.TryGetComponent(component.Owner, out AnimationPlayerComponent? animPlayer)) return;
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
