using System;
using Content.Shared.Vapor;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Chemistry.Visualizers
{
    [UsedImplicitly]
    public sealed class VaporVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string AnimationKey = "flick_animation";

        [DataField("animation_time")]
        private float _delay = 0.25f;

        [DataField("animation_state")]
        private string _state = "chempuff";

        private Animation VaporFlick = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            VaporFlick = new Animation {Length = TimeSpan.FromSeconds(_delay)};
            {
                var flick = new AnimationTrackSpriteFlick();
                VaporFlick.AnimationTracks.Add(flick);
                flick.LayerKey = VaporVisualLayers.Base;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(_state, 0f));
            }
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

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

            var animPlayer = IoCManager.Resolve<IEntityManager>().GetComponent<AnimationPlayerComponent>(component.Owner);

            if(!animPlayer.HasRunningAnimation(AnimationKey))
                animPlayer.Play(VaporFlick, AnimationKey);
        }

        private void SetColor(AppearanceComponent component, Color color)
        {
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

            sprite.Color = color;
        }
    }

    public enum VaporVisualLayers : byte
    {
        Base
    }
}
