using System;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;

namespace Content.Client.Light
{
    [UsedImplicitly]
    public class LanternVisualizer : AppearanceVisualizer
    {
        private readonly Animation _radiatingLightAnimation = new()
        {
            Length = TimeSpan.FromSeconds(5),
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
                        new AnimationTrackProperty.KeyFrame(2.0f, 1.5f),
                        new AnimationTrackProperty.KeyFrame(3.0f, 3f)
                    }
                }
            }
        };

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            PlayAnimation(component);
        }

        private void PlayAnimation(AppearanceComponent component)
        {
            component.Owner.EnsureComponent(out AnimationPlayerComponent animationPlayer);
            if (animationPlayer.HasRunningAnimation("radiatingLight")) return;
            animationPlayer.Play(_radiatingLightAnimation, "radiatingLight");
            animationPlayer.AnimationCompleted += s => animationPlayer.Play(_radiatingLightAnimation, s);
        }
    }
}
