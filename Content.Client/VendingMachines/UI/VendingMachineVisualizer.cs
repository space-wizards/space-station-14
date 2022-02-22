using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using static Content.Shared.VendingMachines.SharedVendingMachineComponent;

namespace Content.Client.VendingMachines.UI
{
    [UsedImplicitly]
    public sealed class VendingMachineVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        // TODO: Should default to off or broken if damaged
        //

        // TODO: The length of these animations is supposed to be dictated
        // by the vending machine's pack prototype's `AnimationDuration`
        // but we have no good way of passing that data from the server
        // to the client at the moment. Rework Visualizers?

        private Dictionary<string, bool> _baseStates = new();

        private static readonly Dictionary<string, VendingMachineVisualLayers> LayerMap =
            new()
            {
                {"off", VendingMachineVisualLayers.Unlit},
                {"screen", VendingMachineVisualLayers.Screen},
                {"normal", VendingMachineVisualLayers.Base},
                {"normal-unshaded", VendingMachineVisualLayers.BaseUnshaded},
                {"eject", VendingMachineVisualLayers.Base},
                {"eject-unshaded", VendingMachineVisualLayers.BaseUnshaded},
                {"deny", VendingMachineVisualLayers.Base},
                {"deny-unshaded", VendingMachineVisualLayers.BaseUnshaded},
                {"broken", VendingMachineVisualLayers.Unlit},
            };

        [DataField("screen")]
        private bool _screen;

        [DataField("normal")]
        private bool _normal;

        [DataField("normalUnshaded")]
        private bool _normalUnshaded;

        [DataField("eject")]
        private bool _eject;

        [DataField("ejectUnshaded")]
        private bool _ejectUnshaded;

        [DataField("deny")]
        private bool _deny;

        [DataField("denyUnshaded")]
        private bool _denyUnshaded;

        [DataField("broken")]
        private bool _broken;

        [DataField("brokenUnshaded")]
        private bool _brokenUnshaded;

        private readonly Dictionary<string, Animation> _animations = new();

        void ISerializationHooks.AfterDeserialization()
        {
            // Used a dictionary so the yaml can adhere to the style-guide and the texture states can be clear
            var states = new Dictionary<string, bool>
            {
                {"off", true},
                {"screen", _screen},
                {"normal", _normal},
                {"normal-unshaded", _normalUnshaded},
                {"eject", _eject},
                {"eject-unshaded", _ejectUnshaded},
                {"deny", _deny},
                {"deny-unshaded", _denyUnshaded},
                {"broken", _broken},
                {"broken-unshaded", _brokenUnshaded},
            };

            _baseStates = states;

            if (_baseStates["deny"])
            {
                InitializeAnimation("deny");
            }

            if (_baseStates["deny-unshaded"])
            {
                InitializeAnimation("deny-unshaded", true);
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
            flick.LayerKey = unshaded ? VendingMachineVisualLayers.BaseUnshaded : VendingMachineVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(key, 0f));
        }

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            IoCManager.Resolve<IEntityManager>().EnsureComponent<AnimationPlayerComponent>(entity);
        }

        private void HideLayers(ISpriteComponent spriteComponent)
        {
            foreach (var layer in spriteComponent.AllLayers)
            {
                layer.Visible = false;
            }

            spriteComponent.LayerSetVisible(VendingMachineVisualLayers.Unlit, true);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entMan = IoCManager.Resolve<IEntityManager>();
            var sprite = entMan.GetComponent<ISpriteComponent>(component.Owner);
            var animPlayer = entMan.GetComponent<AnimationPlayerComponent>(component.Owner);
            if (!component.TryGetData(VendingMachineVisuals.VisualState, out VendingMachineVisualState state))
            {
                state = VendingMachineVisualState.Normal;
            }

            // Hide last state
            HideLayers(sprite);
            ActivateState(sprite, "off");

            switch (state)
            {
                case VendingMachineVisualState.Normal:
                    ActivateState(sprite,  "screen");
                    ActivateState(sprite, "normal-unshaded");
                    ActivateState(sprite, "normal");
                    break;

                case VendingMachineVisualState.Off:
                    break;

                case VendingMachineVisualState.Broken:
                    ActivateState(sprite, "broken-unshaded");
                    ActivateState(sprite, "broken");

                    break;
                case VendingMachineVisualState.Deny:
                    ActivateState(sprite,  "screen");
                    ActivateAnimation(sprite, animPlayer, "deny-unshaded");
                    ActivateAnimation(sprite, animPlayer, "deny");

                    break;
                case VendingMachineVisualState.Eject:
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

        public enum VendingMachineVisualLayers : byte
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
