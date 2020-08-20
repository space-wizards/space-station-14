using System;
using System.Runtime.InteropServices.ComTypes;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class RadiatingLightVisualizer : AppearanceVisualizer
    {
        private bool _playing = false;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            if (node.TryGetNode("playing", out var playing))
            {
                _playing = playing.AsBool();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (component.TryGetData<bool>(HandheldLightVisuals.LowPower, out bool state) &&
                !_playing)
            {
                PlayAnimation(component, state);
            }
        }

        public void PlayAnimation(AppearanceComponent component, bool state)
        {
            component.Owner.EnsureComponent(out AnimationPlayerComponent animationPlayer);

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
                            new AnimationTrackProperty.KeyFrame(4.0f, 1),
                            new AnimationTrackProperty.KeyFrame(3.0f, 2)
                        }
                    }
                }
            };
            animationPlayer.Play(animation, "radiatingLight");
            animationPlayer.AnimationCompleted += s => animationPlayer.Play(animation, s);
            _playing = true;

        }
    }
}
