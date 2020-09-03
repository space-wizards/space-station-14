using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using static Content.Shared.GameObjects.Components.SharedPipeDispenserComponent;

namespace Content.Client.GameObjects.Components.VendingMachines
{
    [UsedImplicitly]
    public sealed class PipeDispenserVisualizer : AppearanceVisualizer
    {
        private Dictionary<string, bool> _baseStates;

        private static readonly Dictionary<string, PipeDispenserVisualLayers> LayerMap =
            new Dictionary<string, PipeDispenserVisualLayers>
            {
                {"off", PipeDispenserVisualLayers.Unlit},
                {"screen", PipeDispenserVisualLayers.Screen},
                {"normal", PipeDispenserVisualLayers.Base},
                {"normal-unshaded", PipeDispenserVisualLayers.BaseUnshaded},
                {"eject", PipeDispenserVisualLayers.Base},
                {"eject-unshaded", PipeDispenserVisualLayers.BaseUnshaded},
                {"broken", PipeDispenserVisualLayers.Unlit},
            };

        private Dictionary<string, Animation> _animations = new Dictionary<string, Animation>();

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _baseStates = new Dictionary<string, bool>
            {
                {"off", true},
            };

            // Used a dictionary so the yaml can adhere to the style-guide and the texture states can be clear
            var states = new Dictionary<string, string>
            {
                {"screen", "screen"},
                {"normal", "normal"},
                {"normalUnshaded", "normal-unshaded"},
                {"eject", "eject"},
                {"ejectUnshaded", "eject-unshaded"},
                {"broken", "broken"},
                {"brokenUnshaded", "broken-unshaded"},
            };

            foreach (var (state, textureState) in states)
            {
                if (!node.TryGetNode(state, out var yamlNode))
                {
                    _baseStates[textureState] = false;
                    continue;
                }

                _baseStates.Add(textureState, yamlNode.AsBool());
            }

            if (_baseStates["eject"])
            {
                InitializeAnimation("eject");
            }

            if (_baseStates["eject-unshaded"])
            {
                InitializeAnimation("eject-unshaded", true);
            }
        }

        private void InitializeAnimation(string key, bool unshaded = false)
        {
            _animations.Add(key, new Animation {Length = TimeSpan.FromSeconds(1.2f)});

            var flick = new AnimationTrackSpriteFlick();
            _animations[key].AnimationTracks.Add(flick);
            flick.LayerKey = unshaded ? PipeDispenserVisualLayers.BaseUnshaded : PipeDispenserVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(key, 0f));
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.HasComponent<AnimationPlayerComponent>())
            {
                entity.AddComponent<AnimationPlayerComponent>();
            }
        }

        private void HideLayers(ISpriteComponent spriteComponent)
        {
            foreach (var layer in spriteComponent.AllLayers)
            {
                layer.Visible = false;
            }

            spriteComponent.LayerSetVisible(PipeDispenserVisualLayers.Unlit, true);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();
            if (!component.TryGetData(PipeDispenserVisuals.VisualState, out PipeDispenserVisualState state))
            {
                state = PipeDispenserVisualState.Normal;
            }

            // Hide last state
            HideLayers(sprite);
            ActivateState(sprite, "off");

            switch (state)
            {
                case PipeDispenserVisualState.Normal:
                    ActivateState(sprite,  "screen");
                    ActivateState(sprite, "normal-unshaded");
                    ActivateState(sprite, "normal");
                    break;

                case PipeDispenserVisualState.Off:
                    break;

                case PipeDispenserVisualState.Broken:
                    ActivateState(sprite, "broken-unshaded");
                    ActivateState(sprite, "broken");

                    break;
                case PipeDispenserVisualState.Eject:
                    ActivateState(sprite,  "screen");
                    ActivateAnimation(sprite, animPlayer, "eject-unshaded");
                    ActivateAnimation(sprite, animPlayer, "eject");

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Helper methods just to avoid all of that hard-to-read-indented code
        private void ActivateState(ISpriteComponent spriteComponent, string stateId)
        {
            // No state for it on the rsi :(
            if (!_baseStates[stateId])
            {
                return;
            }

            var stateLayer = LayerMap[stateId];
            spriteComponent.LayerSetVisible(stateLayer, true);
            spriteComponent.LayerSetState(stateLayer, stateId);
        }

        private void ActivateAnimation(ISpriteComponent spriteComponent, AnimationPlayerComponent animationPlayer, string key)
        {
            if (!_animations.TryGetValue(key, out var animation))
            {
                return;
            }

            if (!animationPlayer.HasRunningAnimation(key))
            {
                spriteComponent.LayerSetVisible(LayerMap[key], true);
                animationPlayer.Play(animation, key);
            }
        }

        public enum PipeDispenserVisualLayers
        {
            // Off / Broken. The other layers will overlay this if the machine is on.
            Unlit,
            // Normal / Deny / Eject
            Base,
            BaseUnshaded,
            // Screens that are persistent (where the machine is not off or broken)
            Screen,
        }
    }
}
