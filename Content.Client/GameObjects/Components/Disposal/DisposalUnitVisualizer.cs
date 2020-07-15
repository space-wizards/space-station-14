using System;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalUnitComponent;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class DisposalUnitVisualizer : AppearanceVisualizer
    {
        private const string AnimationKey = "disposal_unit_animation";

        private string _stateAnchored;
        private string _stateUnAnchored;
        private string _stateFlush;

        private Animation _flushAnimation;

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.TryGetData(Visuals.VisualState, out VisualState state))
            {
                return;
            }

            if (!appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            var animPlayer = appearance.Owner.GetComponent<AnimationPlayerComponent>();

            switch (state)
            {
                case VisualState.Flushing:
                    if (!animPlayer.HasRunningAnimation(AnimationKey))
                    {
                        animPlayer.Play(_flushAnimation, AnimationKey);
                    }

                    break;
                case VisualState.UnAnchored:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, _stateUnAnchored);
                    break;
                case VisualState.Anchored:
                    sprite.LayerSetState(DisposalUnitVisualLayers.Base, _stateAnchored);
                    break;
            }
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("state_anchored", out var child))
            {
                _stateAnchored = child.AsString();
            }

            if (node.TryGetNode("state_unanchored", out child))
            {
                _stateUnAnchored = child.AsString();
            }

            if (node.TryGetNode("state_flush", out child))
            {
                _stateFlush = child.AsString();
            }

            var flushSound = node.GetNode("flush_sound").AsString();
            var flushTime = node.GetNode("flush_time").AsFloat();

            _flushAnimation = new Animation {Length = TimeSpan.FromSeconds(flushTime)};

            var flick = new AnimationTrackSpriteFlick();
            _flushAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = DisposalUnitVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(_stateFlush, 0));

            var sound = new AnimationTrackPlaySound();
            _flushAnimation.AnimationTracks.Add(sound);
            sound.KeyFrames.Add(new AnimationTrackPlaySound.KeyFrame(flushSound, 0));
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            entity.EnsureComponent<AnimationPlayerComponent>();
            var appearance = entity.EnsureComponent<AppearanceComponent>();

            ChangeState(appearance);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Owner.Deleted)
            {
                return;
            }

            ChangeState(component);
        }
    }

    public enum DisposalUnitVisualLayers
    {
        Base
    }
}
