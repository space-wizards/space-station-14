#nullable enable
using System;
using Content.Shared.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Chemistry
{
    [UsedImplicitly]
    public class FoamVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "foamdissolve_animation";
        private Animation _foamDissolve = new();
        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var delay = 0.6f;
            var state = "foam-dissolve";

            if (node.TryGetNode("animationTime", out var delayNode))
            {
                delay = delayNode.AsFloat();
            }

            if (node.TryGetNode("animationState", out var stateNode))
            {
                state = stateNode.AsString();
            }

            _foamDissolve = new Animation {Length = TimeSpan.FromSeconds(delay)};
            var flick = new AnimationTrackSpriteFlick();
            _foamDissolve.AnimationTracks.Add(flick);
            flick.LayerKey = FoamVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(state, 0f));
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<bool>(FoamVisuals.State, out var state))
            {
                if (state)
                {
                    if (component.Owner.TryGetComponent(out AnimationPlayerComponent? animPlayer))
                    {
                        if (!animPlayer.HasRunningAnimation(AnimationKey))
                            animPlayer.Play(_foamDissolve, AnimationKey);
                    }
                }
            }

            if (component.TryGetData<Color>(FoamVisuals.Color, out var color))
            {
                if (component.Owner.TryGetComponent(out ISpriteComponent? sprite))
                {
                    sprite.Color = color;
                }
            }
        }
    }

    public enum FoamVisualLayers : byte
    {
        Base
    }
}
