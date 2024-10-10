using Content.Client.Atmos.Components;
using Robust.Client.GameObjects;
using Content.Shared.Atmos.Piping;
using Content.Shared.Hands;
using Content.Shared.Atmos.Components;

namespace Content.Client.Atmos.EntitySystems;

public sealed class PipeColorVisualizerSystem : VisualizerSystem<PipeColorVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeColorVisualsComponent, GetInhandVisualsEvent>(OnGetVisuals);
    }

    private void OnGetVisuals(EntityUid uid, PipeColorVisualsComponent item, GetInhandVisualsEvent args)
    {
        foreach (var (key, layerData) in args.Layers)
        {
            if (TryComp(uid, out AtmosPipeColorComponent? pipeColor))
            {
                layerData.Color = pipeColor.Color;
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, PipeColorVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite)
            && AppearanceSystem.TryGetData<Color>(uid, PipeColorVisuals.Color, out var color, args.Component))
        {
            // T-ray scanner / sub floor runs after this visualizer. Lets not bulldoze transparency.
            var layer = sprite[PipeVisualLayers.Pipe];
            layer.Color = color.WithAlpha(layer.Color.A);
        }
    }
}

public enum PipeVisualLayers : byte
{
    Pipe,
}
