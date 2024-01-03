using System.Linq;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Paint;
using Content.Shared.Humanoid;

namespace Content.Client.Paint
{
    public sealed class PaintedVisualizerSystem : VisualizerSystem<PaintedComponent>
    {
        /// <summary>
        /// Visualizer for Paint which applies a shader and colors the entity.
        /// </summary>

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaintedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
            SubscribeLocalEvent<PaintedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        }

        protected override void OnAppearanceChange(EntityUid uid, PaintedComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var layer = 0;


            for (layer = 0; layer < args.Sprite.AllLayers.Count(); ++layer)
            {
                component.BeforePaintedColor = args.Sprite.Color;

                if (component.Enabled == true)
                {
                    args.Sprite.LayerSetShader(layer, component.ShaderName);
                    args.Sprite.LayerSetColor(layer, component.Color);
                }
                else if (component.Enabled == false)
                {
                    args.Sprite.LayerSetColor(layer, component.BeforePaintedColor);
                    args.Sprite.LayerSetShader(layer, component.BeforePaintedShader);
                }
            }
        }

        // Shader and Color for the held sprites.
        private void OnHeldVisualsUpdated(EntityUid uid, PaintedComponent component, HeldVisualsUpdatedEvent args)
        {
            if (args.RevealedLayers.Count == 0)
            {
                return;
            }

            if (HasComp<HumanoidAppearanceComponent>(uid))
            {
                return;
            }

            if (!TryComp(args.User, out SpriteComponent? sprite))
                return;

            foreach (var revealed in args.RevealedLayers)
            {
                if (!sprite.LayerMapTryGet(revealed, out var layer) || sprite[layer] is not Layer notlayer)
                    continue;

                if (component.Enabled == true)
                {
                    sprite.LayerSetShader(layer, component.ShaderName);
                    sprite.LayerSetColor(layer, component.Color);
                    return;
                }
                else
                    sprite.LayerSetColor(layer, component.BeforePaintedColor);
                return;
            }
        }

        // shader and color for the clothing equipped sprites.
        private void OnEquipmentVisualsUpdated(EntityUid uid, PaintedComponent component, EquipmentVisualsUpdatedEvent args)
        {
            if (args.RevealedLayers.Count == 0)
            {
                return;
            }

            if (!TryComp(args.Equipee, out SpriteComponent? sprite))
                return;

            foreach (var revealed in args.RevealedLayers)
            {
                if (!sprite.LayerMapTryGet(revealed, out var layer) || sprite[layer] is not Layer notlayer)
                    continue;

                if (component.Enabled == true)
                {
                    sprite.LayerSetShader(layer, component.ShaderName);
                    sprite.LayerSetColor(layer, component.Color);
                    return;
                }
                else
                    sprite.LayerSetColor(layer, component.BeforePaintedColor);
                return;
            }
        }
    }
}
