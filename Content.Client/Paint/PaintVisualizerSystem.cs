using System.Linq;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Paint;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Paint
{
    public sealed class PaintedVisualizerSystem : VisualizerSystem<PaintedComponent>
    {
        /// <summary>
        /// Visualizer for Paint which applies a shader and colors the entity.
        /// </summary>

        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        public ShaderInstance? Shader; // in Robust.Client.Graphics so cannot move to shared component.

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaintedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
            SubscribeLocalEvent<PaintedComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PaintedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        }

        protected override void OnAppearanceChange(EntityUid uid, PaintedComponent component, ref AppearanceChangeEvent args)
        {
            // ShaderPrototype sadly in Robust.Client, cannot move to shared component.
            Shader = _protoMan.Index<ShaderPrototype>(component.ShaderName).Instance();

            if (args.Sprite == null)
                return;

            if (!_appearance.TryGetData<bool>(uid, PaintVisuals.Painted, out bool isPainted))
                return;

            var sprite = args.Sprite;


            foreach (var spriteLayer in sprite.AllLayers)
            {
                if (spriteLayer is not Layer layer)
                    continue;

                if (layer.Shader == null) // If shader isn't null we dont want to replace the original shader.
                {
                    layer.Shader = Shader;
                    layer.Color = component.Color;
                }
            }
        }

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

                sprite.LayerSetShader(layer, component.ShaderName);
                sprite.LayerSetColor(layer, component.Color);
            }
        }

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

                sprite.LayerSetShader(layer, component.ShaderName);
                sprite.LayerSetColor(layer, component.Color);
            }
        }

        private void OnShutdown(EntityUid uid, PaintedComponent component, ref ComponentShutdown args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            component.BeforeColor = sprite.Color;
            Shader = _protoMan.Index<ShaderPrototype>(component.ShaderName).Instance();

            if (!Terminating(uid))
            {
                foreach (var spriteLayer in sprite.AllLayers)
                {
                    if (spriteLayer is not Layer layer)
                        continue;

                    if (layer.Shader == Shader) // If shader isn't same as one in component we need to ignore it.
                    {
                        layer.Shader = null;
                        if (layer.Color == component.Color) // If color isn't the same as one in component we don't want to change it.
                            layer.Color = component.BeforeColor;
                    }
                }
            }
        }
    }
}
