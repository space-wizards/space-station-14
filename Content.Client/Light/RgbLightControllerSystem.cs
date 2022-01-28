using System;
using System.Linq;
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
            SubscribeLocalEvent<RgbLightControllerComponent, ComponentShutdown>(OnComponentShutdown);
            SubscribeLocalEvent<RgbLightControllerComponent, ComponentStartup>(OnComponentStart);
        }

        private void OnComponentStart(EntityUid uid, RgbLightControllerComponent rgb, ComponentStartup args)
        {
            if (TryComp(uid, out PointLightComponent? light))
                rgb.OriginalLightColor = light.Color;

            if (TryComp(uid, out SharedItemComponent? item))
                rgb.OriginalItemColor = item.Color;

            GetOriginalSpriteColors(uid, rgb);
        }

        private void OnComponentShutdown(EntityUid uid, RgbLightControllerComponent rgb, ComponentShutdown args)
        {
            if (TryComp(uid, out PointLightComponent? light))
                light.Color = rgb.OriginalLightColor;

            if (TryComp(uid, out SharedItemComponent? item))
                item.Color = rgb.OriginalItemColor;

            ResetSpriteColors(uid, rgb);
        }

        private void OnHandleState(EntityUid uid, RgbLightControllerComponent rgb, ref ComponentHandleState args)
        {
            if (args.Current is not RgbLightControllerState state)
                return;

            ResetSpriteColors(uid, rgb);
            rgb.CycleRate = state.CycleRate;
            rgb.Layers = state.Layers;

            // get the new original sprite colors (necessary if rgb.Layers was updated).
            GetOriginalSpriteColors(uid, rgb);
        }

        private void GetOriginalSpriteColors(EntityUid uid, RgbLightControllerComponent? rgb = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref rgb, ref sprite))
                return;

            if (rgb.Layers == null)
            {
                rgb.OriginalSpriteColor = sprite.Color;
                rgb.OriginalLayerColors = null;
                return;
            }

            var spriteLayerCount = sprite.AllLayers.Count();
            rgb.OriginalLayerColors = new(rgb.Layers.Count);

            foreach (var layer in rgb.Layers.ToArray())
            {
                if (layer < spriteLayerCount)
                    rgb.OriginalLayerColors[layer] = sprite[layer].Color;
                else
                    rgb.Layers.Remove(layer);
            }
        }

        private void ResetSpriteColors(EntityUid uid, RgbLightControllerComponent? rgb = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref rgb, ref sprite))
                return;

            if (rgb.Layers == null || rgb.OriginalLayerColors == null)
            {
                sprite.Color = rgb.OriginalSpriteColor;
                return;
            }

            foreach (var (layer, color) in rgb.OriginalLayerColors)
            {
                sprite.LayerSetColor(layer, color);
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
