using Content.Client.Ensnaring.Components;
using Content.Shared.Ensnaring;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Ensnaring.Visualizers;

public sealed class EnsnareableVisualizerSystem : VisualizerSystem<EnsnareableComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableVisualizerComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, EnsnareableVisualizerComponent component, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(EnsnaredVisualLayers.Ensnared);
    }

    protected override void OnAppearanceChange(EntityUid uid, EnsnareableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Component.TryGetData(EnsnareableVisuals.IsEnsnared, out bool isEnsnared))
        {
            if (args.Sprite != null && component.Sprite != null)
            {
                args.Sprite.LayerSetRSI(EnsnaredVisualLayers.Ensnared, component.Sprite);
                args.Sprite.LayerSetState(EnsnaredVisualLayers.Ensnared, component.State);
                args.Sprite.LayerSetVisible(EnsnaredVisualLayers.Ensnared, isEnsnared);
            }
        }
    }
}

public enum EnsnaredVisualLayers : byte
{
    Ensnared,
}
