using Content.Shared.Light;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Client.Light.Visualizers
{
    [UsedImplicitly]
    public sealed class PoweredLightVisualizer : AppearanceVisualizer
    {
        [DataField("minBlinkingTime")] private float _minBlinkingTime = 0.5f;
        [DataField("maxBlinkingTime")] private float _maxBlinkingTime = 2;
        [DataField("blinkingSound")] private SoundSpecifier? _blinkingSound = default;

        private bool _wasBlinking;

        private Action<string>? _blinkingCallback;

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite)) return;
            if (!component.TryGetData(PoweredLightVisuals.BulbState, out PoweredLightState state)) return;

            switch (state)
            {
                case PoweredLightState.Empty:
                    sprite.LayerSetState(PoweredLightLayers.Base, "empty");
                    ToggleBlinkingAnimation(component, false);
                    break;
                case PoweredLightState.Off:
                    sprite.LayerSetState(PoweredLightLayers.Base, "off");
                    ToggleBlinkingAnimation(component, false);
                    break;
                case PoweredLightState.On:
                    if (component.TryGetData(PoweredLightVisuals.Blinking, out bool isBlinking))
                        ToggleBlinkingAnimation(component, isBlinking);
                    if (!isBlinking)
                    {
                        sprite.LayerSetState(PoweredLightLayers.Base, "on");
                    }
                    break;
                case PoweredLightState.Broken:
                    sprite.LayerSetState(PoweredLightLayers.Base, "broken");
                    ToggleBlinkingAnimation(component, false);
                    break;
                case PoweredLightState.Burned:
                    sprite.LayerSetState(PoweredLightLayers.Base, "burn");
                    ToggleBlinkingAnimation(component, false);
                    break;
            }
        }


        private void ToggleBlinkingAnimation(AppearanceComponent component, bool isBlinking)
        {
            if (isBlinking == _wasBlinking)
                return;
            _wasBlinking = isBlinking;

            component.Owner.EnsureComponent(out AnimationPlayerComponent animationPlayer);

            if (isBlinking)
            {
                _blinkingCallback = (animName) => animationPlayer.Play(BlinkingAnimation(), "blinking");
                animationPlayer.AnimationCompleted += _blinkingCallback;
                animationPlayer.Play(BlinkingAnimation(), "blinking");
            }
            else if (animationPlayer.HasRunningAnimation("blinking"))
            {
                if (_blinkingCallback != null)
                    animationPlayer.AnimationCompleted -= _blinkingCallback;
                animationPlayer.Stop("blinking");
            }
        }

        private Animation BlinkingAnimation()
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var randomTime = random.NextFloat() *
                (_maxBlinkingTime - _minBlinkingTime) + _minBlinkingTime;

            var blinkingAnim = new Animation()
            {
                Length = TimeSpan.FromSeconds(randomTime),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(PointLightComponent),
                        InterpolationMode = AnimationInterpolationMode.Nearest,
                        Property = nameof(PointLightComponent.Enabled),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(false, 0),
                            new AnimationTrackProperty.KeyFrame(true, 1)
                        }
                    },
                    new AnimationTrackSpriteFlick()
                    {
                        LayerKey = PoweredLightLayers.Base,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame("off", 0),
                            new AnimationTrackSpriteFlick.KeyFrame("on", 0.5f)
                        }
                    }
                }
            };

            if (_blinkingSound != null)
            {
                blinkingAnim.AnimationTracks.Add(new AnimationTrackPlaySound()
                {
                    KeyFrames =
                {
                    new AnimationTrackPlaySound.KeyFrame(_blinkingSound.GetSound(), 0.5f)
                }
                });
            }

            return blinkingAnim;
        }
    }
}
