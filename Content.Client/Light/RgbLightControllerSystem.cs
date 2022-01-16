using System;
using Content.Shared.Item;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Light
{
    public sealed class RgbLightControllerSystem : SharedRgbLightControllerSystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RgbLightControllerComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<RgbLightControllerComponent, ComponentRemove>(OnComponentRemoved);
        }

        private void OnComponentRemoved(EntityUid uid, RgbLightControllerComponent rgb, ComponentRemove args)
        {
            if (TryComp(uid, out PointLightComponent? light))
                light.Color = Color.White;

            if (TryComp(uid, out SharedItemComponent? item))
                item.Color = Color.White;

            ResetSpriteColors(uid, rgb);
        }

        private void OnHandleState(EntityUid uid, RgbLightControllerComponent rgb, ref ComponentHandleState args)
        {
            if (args.Current is not RgbLightControllerState state)
                return;

            // just to be safe, un-color the layers so they don't get stuck with some mid-transition one.
            ResetSpriteColors(uid, rgb);

            rgb.CycleRate = state.CycleRate;
            rgb.Layers = state.Layers;
        }

        private void ResetSpriteColors(EntityUid uid, RgbLightControllerComponent? rgb = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref rgb, ref sprite))
                return;

            if (rgb.Layers == null)
            {
                sprite.Color = Color.White;
                return;
            }

            foreach (var layer in rgb.Layers)
            {
                sprite.LayerSetColor(layer, Color.White);
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var (rgb, light, sprite) in EntityManager.EntityQuery<RgbLightControllerComponent, PointLightComponent, SpriteComponent>())
            {
                var color = GetCurrentRgbColor(_gameTiming.RealTime, rgb.CreationTick.Value * _gameTiming.TickPeriod, rgb);

                light.Color = color;

                if (rgb.Layers == null)
                    sprite.Color = color;
                else
                {
                    foreach (var layer in rgb.Layers)
                    {
                        sprite.LayerSetColor(layer, color);
                    }
                }

                // not all rgb is hand-held (Hence, not part of EntityQuery)
                if (TryComp(rgb.Owner, out SharedItemComponent? item))
                    item.Color = color;
            }
        }

        public static Color GetCurrentRgbColor(TimeSpan curTime, TimeSpan offset, RgbLightControllerComponent rgb)
        {
            return Color.FromHsv(new Vector4(
                (float) (((curTime.TotalSeconds - offset.TotalSeconds) * rgb.CycleRate + Math.Abs(rgb.Owner.GetHashCode() * 0.1)) % 1),
                1.0f,
                1.0f,
                1.0f
            ));
        }
    }
}
