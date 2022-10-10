using System;
using Content.Shared.Foam;
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
    public sealed class FoamVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string AnimationKey = "foamdissolve_animation";

        [DataField("animationTime")]
        private float _delay = 0.6f;

        [DataField("animationState")]
        private string _state = "foam-dissolve";

        private Animation _foamDissolve = new();

        void ISerializationHooks.AfterDeserialization()
        {
            _foamDissolve = new Animation {Length = TimeSpan.FromSeconds(_delay)};
            var flick = new AnimationTrackSpriteFlick();
            _foamDissolve.AnimationTracks.Add(flick);
            flick.LayerKey = FoamVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(_state, 0f));
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (component.TryGetData<bool>(FoamVisuals.State, out var state))
            {
                if (state)
                {
                    if (entities.TryGetComponent(component.Owner, out AnimationPlayerComponent? animPlayer))
                    {
                        if (!animPlayer.HasRunningAnimation(AnimationKey))
                            animPlayer.Play(_foamDissolve, AnimationKey);
                    }
                }
            }

            if (component.TryGetData<Color>(FoamVisuals.Color, out var color))
            {
                if (entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
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
