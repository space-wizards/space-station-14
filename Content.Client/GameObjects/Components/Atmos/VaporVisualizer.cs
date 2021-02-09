using System;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class VaporVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "flick_animation";
        private Animation VaporFlick;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var delay = 0.25f;
            var state = "chempuff";

            if (node.TryGetNode("animation_time", out var delayNode))
            {
                delay = delayNode.AsFloat();
            }

            if (node.TryGetNode("animation_state", out var stateNode))
            {
                state = stateNode.AsString();
            }

            VaporFlick = new Animation {Length = TimeSpan.FromSeconds(delay)};
            {
                var flick = new AnimationTrackSpriteFlick();
                VaporFlick.AnimationTracks.Add(flick);
                flick.LayerKey = VaporVisualLayers.Base;
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

            if (component.TryGetData<double>(VaporVisuals.Rotation, out var radians))
            {
                SetRotation(component, new Angle(radians));
            }

            if (component.TryGetData<Color>(VaporVisuals.Color, out var color))
            {
                SetColor(component, color);
            }

            if (component.TryGetData<bool>(VaporVisuals.State, out var state))
            {
                SetState(component, state);
            }
        }

        private void SetState(AppearanceComponent component, bool state)
        {
            if (!state) return;

            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();

            if(!animPlayer.HasRunningAnimation(AnimationKey))
                animPlayer.Play(VaporFlick, AnimationKey);
        }

        private void SetRotation(AppearanceComponent component, Angle rotation)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            sprite.Rotation = rotation;
        }

        private void SetColor(AppearanceComponent component, Color color)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            sprite.Color = color;
        }
    }

    public enum VaporVisualLayers : byte
    {
        Base
    }
}
