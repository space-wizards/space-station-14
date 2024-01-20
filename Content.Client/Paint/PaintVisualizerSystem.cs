using System.Linq;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Paint;

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
            SubscribeLocalEvent<PaintedComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PaintedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        }

        // Applies the shader and color to all sprite layers for the entity.
        protected override void OnAppearanceChange(EntityUid uid, PaintedComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            for (var layer = 0; layer < args.Sprite.AllLayers.Count(); ++layer)
            {
                sprite.LayerSetColor(layer, component.Color);
                sprite.LayerSetShader(layer, component.ShaderName);
            }
        }

        // Shader and Color for the held sprites.
        private void OnHeldVisualsUpdated(EntityUid uid, PaintedComponent component, HeldVisualsUpdatedEvent args)
        {
            if (args.RevealedLayers.Count == 0)
                return;

            if (!TryComp(args.User, out SpriteComponent? sprite))
                return;

            foreach (var revealed in args.RevealedLayers)
            {
                if (!sprite.LayerMapTryGet(revealed, out var layer) || sprite[layer] is not Layer notlayer)
                    continue;

                if (component.Enabled)
                {
                    sprite.LayerSetShader(layer, component.ShaderName);
                    sprite.LayerSetColor(layer, component.Color);
                    return;
                }
                return;
            }
        }

        // shader and color for the clothing equipped sprites.
        private void OnEquipmentVisualsUpdated(EntityUid uid, PaintedComponent component, EquipmentVisualsUpdatedEvent args)
        {
            if (args.RevealedLayers.Count == 0)
                return;

            if (!TryComp(args.Equipee, out SpriteComponent? sprite))
                return;

            foreach (var revealed in args.RevealedLayers)
            {
                if (!sprite.LayerMapTryGet(revealed, out var layer) || sprite[layer] is not Layer notlayer)
                    continue;

                if (component.Enabled)
                {
                    sprite.LayerSetShader(layer, component.ShaderName);
                    sprite.LayerSetColor(layer, component.Color);
                    return;
                }
            }
        }

        // Removes the shader and color from the sprite layers when component is removed. 
        private void OnShutdown(EntityUid uid, PaintedComponent component, ref ComponentShutdown args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            component.BeforeColor = sprite.Color;

            if (!Terminating(uid))
            {
                for (var layer = 0; layer < sprite.AllLayers.Count(); ++layer)
                {
                    sprite.LayerSetShader(layer, null, null);
                    sprite.LayerSetColor(layer, component.BeforeColor);
                }
            }
        }
    }
}
