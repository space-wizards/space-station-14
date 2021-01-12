using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Chemistry
{
    public class FoamVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "foamdisolve_animation";
        private Animation FoamDisolve;
        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var delay = 0.6f;
            var state = "foam-disolve";

            if (node.TryGetNode("animation_time", out var delayNode))
            {
                delay = delayNode.AsFloat();
            }

            if (node.TryGetNode("animation_state", out var stateNode))
            {
                state = stateNode.AsString();
            }

            FoamDisolve = new Animation {Length = TimeSpan.FromSeconds(delay)};
            {
                var flick = new AnimationTrackSpriteFlick();
                FoamDisolve.AnimationTracks.Add(flick);
                flick.LayerKey = FoamVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(state, 0f));
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (component.Deleted)
            {
                return;
            }

            if (component.TryGetData<bool>(FoamVisuals.State, out var state))
            {
                if (state)
                {
                    var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();

                    if(!animPlayer.HasRunningAnimation(AnimationKey))
                        animPlayer.Play(FoamDisolve, AnimationKey);
                }
            }

            if (component.TryGetData<Color>(FoamVisuals.Color, out var color))
            {
                var sprite = component.Owner.GetComponent<ISpriteComponent>();
                sprite.Color = color;
            }
        }
    }

    public enum FoamVisualLayers : byte
    {
        Base
    }
}
