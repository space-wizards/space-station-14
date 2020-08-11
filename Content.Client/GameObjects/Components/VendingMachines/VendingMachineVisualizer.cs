using System;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using YamlDotNet.RepresentationModel;
using static Content.Shared.GameObjects.Components.VendingMachines.SharedVendingMachineComponent;

namespace Content.Client.GameObjects.Components.VendingMachines
{
    public class VendingMachineVisualizer : AppearanceVisualizer
    {
        // TODO: The length of these animations is supposed to be dictated
        // by the vending machine's pack prototype's `AnimationDuration`
        // but we have no good way of passing that data from the server
        // to the client at the moment. Rework Visualizers?
        private const string DeniedAnimationKey = "deny";
        private const string EjectAnimationKey = "eject";

        private Animation _deniedAnimation;
        private Animation _ejectAnimation;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            _deniedAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                _deniedAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = VendingMachineVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny", 0f));
            }

            _ejectAnimation = new Animation {Length = TimeSpan.FromSeconds(1.2f)};
            {
                var flick = new AnimationTrackSpriteFlick();
                _ejectAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = VendingMachineVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("eject", 0f));
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.HasComponent<AnimationPlayerComponent>())
            {
                entity.AddComponent<AnimationPlayerComponent>();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            var animPlayer = component.Owner.GetComponent<AnimationPlayerComponent>();
            if (!component.TryGetData(VendingMachineVisuals.VisualState, out VendingMachineVisualState state))
            {
                state = VendingMachineVisualState.Normal;
            }

            switch (state)
            {
                case VendingMachineVisualState.Normal:
                    sprite.LayerSetState(VendingMachineVisualLayers.Base, "normal");
                    break;
                case VendingMachineVisualState.Off:
                    sprite.LayerSetState(VendingMachineVisualLayers.Base, "off");
                    break;
                case VendingMachineVisualState.Broken:
                    sprite.LayerSetState(VendingMachineVisualLayers.Base, "broken");
                    break;
                case VendingMachineVisualState.Deny:
                    if (!animPlayer.HasRunningAnimation(DeniedAnimationKey))
                    {
                        animPlayer.Play(_deniedAnimation, DeniedAnimationKey);
                    }

                    break;
                case VendingMachineVisualState.Eject:
                    if (!animPlayer.HasRunningAnimation(EjectAnimationKey))
                    {
                        animPlayer.Play(_ejectAnimation, EjectAnimationKey);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum VendingMachineVisualLayers
        {
            Base,
        }
    }
}
