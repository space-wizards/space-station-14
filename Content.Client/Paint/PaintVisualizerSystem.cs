using System.Linq;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Robust.Client.Graphics;
using Content.Shared.Paint;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using System.Reflection.Metadata;

namespace Content.Client.Paint
{
    public sealed class PaintedVisualizerSystem : VisualizerSystem<PaintedComponent>
    {
        /// <summary>
        /// Visualizer for Paint which applies a shader and colors the entity.
        /// </summary>

        [Dependency] private readonly IPrototypeManager _proto = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaintedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
            SubscribeLocalEvent<PaintedComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PaintedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        }

        protected override void OnAppearanceChange(EntityUid uid, PaintedComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var shader = _proto.Index<ShaderPrototype>("Colored").InstanceUnique();

            shader.SetParameter("color", component.Color);
            args.Sprite.PostShader = component.Enabled ? shader : null;

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
            }
        }

        private void OnShutdown(EntityUid uid, PaintedComponent component, ref ComponentShutdown args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            if (!Terminating(uid))
            {
                var shader = _proto.Index<ShaderPrototype>("Colored").InstanceUnique();

                shader.SetParameter("color", component.Color);
                sprite.PostShader = false ? shader : null;
            }
        }
    }
}
