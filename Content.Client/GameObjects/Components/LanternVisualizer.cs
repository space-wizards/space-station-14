using System;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class LanternVisualizer : AppearanceVisualizer
    {
        private string _powerSource;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            if (node.TryGetNode("PowerSource", out var powerSource))
            {
                _powerSource = powerSource.AsString();
            }
        }

        private Animation radiatingLightAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(1),
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
                        new AnimationTrackProperty.KeyFrame(2.0f, 0.5f),
                        new AnimationTrackProperty.KeyFrame(3.0f, 1)
                    }
                }
            }
        };

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (component.Deleted)
            {
                return;
            }

            PlayAnimation(component);
        }

        public void PlayAnimation(AppearanceComponent component)
        {
            component.Owner.EnsureComponent(out AnimationPlayerComponent animationPlayer);
            if (!animationPlayer.HasRunningAnimation("radiatingLight"))
            {
                animationPlayer.Play(radiatingLightAnimation, "radiatingLight");
                animationPlayer.AnimationCompleted += s => animationPlayer.Play(radiatingLightAnimation, s);
            }
        }
    }
}
