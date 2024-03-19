using System.Linq;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Paint;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Paint;

public sealed class PaintedVisualizerSystem : VisualizerSystem<PaintedComponent>
{
    /// <summary>
    /// Visualizer for Paint which applies a shader and colors the entity.
    /// </summary>

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<PaintedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PaintedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    protected override void OnAppearanceChange(EntityUid uid, PaintedComponent component, ref AppearanceChangeEvent args)
    {
        var shader = _protoMan.Index<ShaderPrototype>(component.ShaderName).Instance();

        if (args.Sprite == null)
            return;

        // What is this even doing? It's not even checking what the value is.
        if (!_appearance.TryGetData(uid, PaintVisuals.Painted, out bool isPainted))
            return;

        var sprite = args.Sprite;

        foreach (var spriteLayer in sprite.AllLayers)
        {
            if (spriteLayer is not Layer layer)
                continue;

            if (layer.Shader == null) // If shader isn't null we dont want to replace the original shader.
            {
                layer.Shader = shader;
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
            if (!sprite.LayerMapTryGet(revealed, out var layer))
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
            if (!sprite.LayerMapTryGet(revealed, out var layer))
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
        var shader = _protoMan.Index<ShaderPrototype>(component.ShaderName).Instance();

        if (!Terminating(uid))
        {
            foreach (var spriteLayer in sprite.AllLayers)
            {
                if (spriteLayer is not Layer layer)
                    continue;

                if (layer.Shader == shader) // If shader isn't same as one in component we need to ignore it.
                {
                    layer.Shader = null;
                    if (layer.Color == component.Color) // If color isn't the same as one in component we don't want to change it.
                        layer.Color = component.BeforeColor;
                }
            }
        }
    }
}
