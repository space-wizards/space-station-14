using Content.Client.Atmos.Components;
using Robust.Client.GameObjects;
using Content.Shared.Atmos.Piping;
using Content.Shared.Hands;
using Content.Shared.Atmos.Components;
using Content.Shared.Item;

namespace Content.Client.Atmos.EntitySystems;

public sealed class PipeColorVisualizerSystem : VisualizerSystem<PipeColorVisualsComponent>
{
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeColorVisualsComponent, GetInhandVisualsEvent>(OnGetVisuals);
    }

    private void OnGetVisuals(Entity<PipeColorVisualsComponent> item, ref GetInhandVisualsEvent args)
    {
        foreach (var (key, layerData) in args.Layers)
        {
            if (TryComp(item.Owner, out AtmosPipeColorComponent? pipeColor))
                layerData.Color = pipeColor.Color;
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

            sprite.Color = color;
        }

        _itemSystem.VisualsChanged(uid);
    }
}

public enum PipeVisualLayers : byte
{
    Pipe,
}
