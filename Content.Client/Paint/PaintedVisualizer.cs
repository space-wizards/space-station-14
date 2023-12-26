using System.Linq;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Paint;
using Content.Shared.Humanoid;

namespace Content.Client.Paint
{
    public sealed class PaintVisualizerSystem : VisualizerSystem<PaintedComponent>
    {
        /// <summary>
        /// Visualizer for Paint which applies a shader and colors the entity.
        /// </summary>

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaintedComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<PaintedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
            SubscribeLocalEvent<PaintedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        }

        // On component startup sets the shading and color for the icon sprite.
        private void OnStartup(EntityUid uid, PaintedComponent component, ComponentStartup args)
        {
            if (HasComp<HumanoidAppearanceComponent>(uid))
                return;

            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;


            for (var layer = 0; layer < sprite.AllLayers.Count(); layer++)
            {
                sprite.LayerSetShader(layer, component.ShaderName);
                sprite.LayerSetColor(layer, component.Color);
            }
        }

        // Shader and Color for the held sprites.
        private void OnHeldVisualsUpdated(EntityUid uid, PaintedComponent component, HeldVisualsUpdatedEvent args)
        {
            if (args.RevealedLayers.Count == 0)
            {
                return;
            }

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

                sprite.LayerSetShader(layer, component.ShaderName);
                sprite.LayerSetColor(layer, component.Color);
            }
        }
    }
}
